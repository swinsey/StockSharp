namespace StockSharp.SmartCom
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Interop;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.SmartCom.Native;
	using StockSharp.Localization;

	/// <summary>
	/// Адаптер сообщений для SmartCOM.
	/// </summary>
	public partial class SmartComMessageAdapter : MessageAdapter<SmartComSessionHolder>
	{
		private ISmartComWrapper _wrapper;

		/// <summary>
		/// Создать <see cref="SmartComMessageAdapter"/>.
		/// </summary>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		public SmartComMessageAdapter(SmartComSessionHolder sessionHolder)
			: base(sessionHolder)
		{
			SessionHolder.VersionChanged += OnSessionVersionChanged;

			PortfolioBoardCodes = new Dictionary<string, string>
			{
			    { "EQ", ExchangeBoard.MicexEqbr.Code },
			    { "FOB", ExchangeBoard.MicexFbcb.Code },
			    { "RTS_FUT", ExchangeBoard.Forts.Code },
			};

			OnSessionVersionChanged();
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			SessionHolder.VersionChanged -= OnSessionVersionChanged;

			base.DisposeManaged();
		}

		private void OnSessionVersionChanged()
		{
			Platform = SessionHolder.Version == SmartComVersions.V3 ? Platforms.AnyCPU : Platforms.x86;
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="PortfolioLookupMessage"/> для получения списка портфелей и позиций.
		/// </summary>
		public override bool PortfolioLookupRequired
		{
			get { return true; }
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="SecurityLookupMessage"/> для получения списка инструментов.
		/// </summary>
		public override bool SecurityLookupRequired
		{
			get { return true; }
		}

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Connect:
				{
					if (_wrapper != null)
						throw new InvalidOperationException(LocalizedStrings.Str1619);

					_tempDepths.Clear();
					_candleTransactions.Clear();
					_bestQuotes.Clear();

					_lookupSecuritiesId = 0;
					_lookupPortfoliosId = 0;

					//_smartOrderIds.Clear();
					//_smartIdOrders.Clear();

					switch (SessionHolder.Version)
					{
						case SmartComVersions.V2:
							_wrapper = new SmartCom2Wrapper();
							break;
						case SmartComVersions.V3:
							_wrapper = (Environment.Is64BitProcess
								? (ISmartComWrapper)new SmartCom3Wrapper64
								{
									ClientSettings = SessionHolder.ClientSettings,
									ServerSettings = SessionHolder.ServerSettings,
								}
								: new SmartCom3Wrapper32
								{
									ClientSettings = SessionHolder.ClientSettings,
									ServerSettings = SessionHolder.ServerSettings,
								});

							break;
						default:
							throw new ArgumentOutOfRangeException();
					}

					_wrapper.NewPortfolio += OnNewPortfolio;
					_wrapper.PortfolioChanged += OnPortfolioChanged;
					_wrapper.PositionChanged += OnPositionChanged;
					_wrapper.NewMyTrade += OnNewMyTrade;
					_wrapper.NewOrder += OnNewOrder;
					_wrapper.OrderFailed += OnOrderFailed;
					_wrapper.OrderCancelFailed += OnOrderCancelFailed;
					_wrapper.OrderChanged += OnOrderChanged;
					_wrapper.OrderReRegisterFailed += OnOrderReRegisterFailed;
					_wrapper.OrderReRegistered += OnOrderReRegistered;

					_wrapper.NewSecurity += OnNewSecurity;
					_wrapper.SecurityChanged += OnSecurityChanged;
					_wrapper.QuoteChanged += OnQuoteChanged;
					_wrapper.NewTrade += OnNewTrade;
					_wrapper.NewHistoryTrade += OnNewHistoryTrade;
					_wrapper.NewBar += OnNewBar;

					_wrapper.Connected += OnConnected;
					_wrapper.Disconnected += OnDisconnected;

					_wrapper.Connect(SessionHolder.Address.GetHost(), (short)SessionHolder.Address.GetPort(), SessionHolder.Login, SessionHolder.Password.To<string>());

					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_wrapper == null)
						throw new InvalidOperationException(LocalizedStrings.Str1856);

					_wrapper.Disconnect();

					break;
				}

				case MessageTypes.OrderRegister:
					ProcessRegisterMessage((OrderRegisterMessage)message);
					break;

				case MessageTypes.OrderCancel:
					ProcessCancelMessage((OrderCancelMessage)message);
					break;

				case MessageTypes.OrderGroupCancel:
					_wrapper.CancelAllOrders();
					break;

				case MessageTypes.OrderReplace:
					ProcessReplaceMessage((OrderReplaceMessage)message);
					break;

				case MessageTypes.Portfolio:
					ProcessPortfolioMessage((PortfolioMessage)message);
					break;

				case MessageTypes.PortfolioLookup:
					ProcessPortolioLookupMessage((PortfolioLookupMessage)message);
					break;

				case MessageTypes.MarketData:
					ProcessMarketDataMessage((MarketDataMessage)message);
					break;

				case MessageTypes.SecurityLookup:
					ProcessSecurityLookupMessage((SecurityLookupMessage)message);
					break;
			}
		}

		private void OnConnected()
		{
			SendOutMessage(new ConnectMessage());
		}

		private void OnDisconnected(Exception error)
		{
			_wrapper.NewPortfolio -= OnNewPortfolio;
			_wrapper.PortfolioChanged -= OnPortfolioChanged;
			_wrapper.PositionChanged -= OnPositionChanged;
			_wrapper.NewMyTrade -= OnNewMyTrade;
			_wrapper.NewOrder -= OnNewOrder;
			_wrapper.OrderFailed -= OnOrderFailed;
			_wrapper.OrderCancelFailed -= OnOrderCancelFailed;
			_wrapper.OrderChanged -= OnOrderChanged;
			_wrapper.OrderReRegisterFailed -= OnOrderReRegisterFailed;
			_wrapper.OrderReRegistered -= OnOrderReRegistered;

			_wrapper.NewSecurity -= OnNewSecurity;
			_wrapper.SecurityChanged -= OnSecurityChanged;
			_wrapper.QuoteChanged -= OnQuoteChanged;
			_wrapper.NewTrade -= OnNewTrade;
			_wrapper.NewHistoryTrade -= OnNewHistoryTrade;
			_wrapper.NewBar -= OnNewBar;

			_wrapper.Connected -= OnConnected;
			_wrapper.Disconnected -= OnDisconnected;

			SendOutMessage(new DisconnectMessage { Error = error });

			_wrapper = null;
		}
	}
}