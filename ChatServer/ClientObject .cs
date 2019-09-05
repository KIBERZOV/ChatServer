using System;
using System.Net.Sockets;
using System.Text;

namespace ChatServer
{
    class ClientObject
    {
        protected internal string Id { get; private set; }
        protected internal NetworkStream Stream { get; private set; }
        string userName;
        TcpClient client;
        ServerObject server; // объект сервера
        
        /// <summary>
        /// У объекта ClientObject устанавливается свойство Id, которое уникально инидентифицирует, 
        /// и свойство Stream, хранящее поток для взаимодействия с клиентом. 
        /// При создании нового объекта в конструкторе происходит его добавление в коллекцию подключений класса ServerObject.
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <param name="serverObject"></param>
        public ClientObject(TcpClient tcpClient, ServerObject serverObject)
        {
            Id = Guid.NewGuid().ToString();
            client = tcpClient;
            server = serverObject;
            serverObject.AddConnection(this);
        }
        
        /// <summary>
        /// В методе Process реализован простой протокол для обмена сообщениями с клиентом.
        /// Так, как в начале получаем имя подключенного пользователя, а затем, в цикле получаем все остальные сообщения.
        /// Для трансляции сообщений всем пользователям используется метод BroadcastMessage() класса ServerObject.
        /// </summary>
        public void Process()
        {
            try
            {
                // загружаем список доступных команд
                server.AddListTeams();

                Stream = client.GetStream();
                // получаем имя пользователя
                string message = GetMessage();
                userName = message;
                server.AddUserName(message);
                message = userName + " вошел в чат";
                // посылаем сообщение о входе в чат всем подключенным пользователям
                server.BroadcastMessage(message, this.Id);
                Console.WriteLine(message);
                // в бесконечном цикле получаем сообщения от клиента
                while (true)
                {
                    try
                    {
                        message = GetMessage();

                        if(message != "")
                        {
                            switch (message)
                            {
                                case "#Список доступных команд":
                                    message = String.Format("{0}: {1}", "#BOT", server.DataBaseTeams());
                                    Console.WriteLine(message);
                                    server.MessageToSender(message, this.Id);
                                    break;
                                case "#Список пользователей":
                                    message = String.Format("{0}: {1}", "#BOT", server.DataBaseClients());
                                    Console.WriteLine(message);
                                    server.MessageToSender(message, this.Id);
                                    break;
                                case "#Завершить чат":
                                    Console.WriteLine(message);
                                    server.RemoveConnection(this.Id);
                                    Close();
                                    break;
                                default:
                                    message = String.Format("{0}: {1}", userName, message);
                                    Console.WriteLine(message);
                                    server.BroadcastMessage(message, this.Id);
                                    break;
                            }
                        }
                    }
                    catch
                    {
                        message = String.Format("{0}: покинул чат", userName);
                        server.RemoveUserName(userName);
                        Console.WriteLine(message);
                        server.BroadcastMessage(message, this.Id);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                // в случае выхода из цикла закрываем ресурсы
                server.RemoveConnection(this.Id);
                Close();
            }
        }

        /// <summary>
        /// В методе GetMessage мы преобразуем сообщение пользователя, представленное массивом байтов в строку.
        /// </summary>
        /// <returns></returns>
        private string GetMessage()
        {
            byte[] data = new byte[64]; // буфер для получаемых данных
            StringBuilder builder = new StringBuilder();
            int bytes = 0;
            do
            {
                bytes = Stream.Read(data, 0, data.Length);
                builder.Append(Encoding.UTF8.GetString(data, 0, bytes));
            }
            while (Stream.DataAvailable);

            return builder.ToString();
        }

        /// <summary>
        /// Метод Close завершает подключение клиента.
        /// </summary>
        protected internal void Close()
        {
            if (Stream != null)
            if (client != null)
                client.Close();
        }
    }
}
