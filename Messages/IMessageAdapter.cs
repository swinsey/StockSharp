﻿namespace StockSharp.Messages
{
	using System;

	/// <summary>
	/// Адаптер, конвертирующий сообщения <see cref="Message"/> в команды торговой системы и обратно.
	/// </summary>
	public interface IMessageAdapter : IDisposable, IMessageChannel
	{
		/// <summary>
		/// Контейнер для сессии.
		/// </summary>
		IMessageSessionHolder SessionHolder { get; }

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="PortfolioLookupMessage"/> для получения списка портфелей и позиций.
		/// </summary>
		bool PortfolioLookupRequired { get; }

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="SecurityLookupMessage"/> для получения списка инструментов.
		/// </summary>
		bool SecurityLookupRequired { get; }

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="OrderStatusMessage"/> для получения списка заявок и собственных сделок.
		/// </summary>
		bool OrderStatusRequired { get; }

		/// <summary>
		/// Добавить <see cref="Message"/> в исходящую очередь <see cref="IMessageAdapter"/>.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		void SendOutMessage(Message message);

		/// <summary>
		/// Обработчик входящих сообщений.
		/// </summary>
		IMessageProcessor InMessageProcessor { get; set; }

		/// <summary>
		/// Обработчик исходящих сообщений.
		/// </summary>
		IMessageProcessor OutMessageProcessor { get; set; }
	}
}