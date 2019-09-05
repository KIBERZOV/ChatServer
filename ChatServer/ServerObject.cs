using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;

namespace ChatServer
{
    class ServerObject
    {
        static TcpListener tcpListener; // сервер для прослушивания
        List<ClientObject> clients = new List<ClientObject>(); // все подключения
        List<string> UserName = new List<string>(); // все пользователи

        /// <summary>
        /// Метод AddConnection добавляет клиент в коллекцию clients, тем самым подключает его к серверу.
        /// </summary>
        /// <param name="clientObject"></param>
        protected internal void AddConnection(ClientObject clientObject)
        {
            clients.Add(clientObject);
        }

        /// <summary>
        /// Метод AddUserName добавляет имя пользователя в коллекцию UserName.
        /// Данный метод необходим для статистики.
        /// </summary>
        /// <param name="userName"></param>
        protected internal void AddUserName(string userName)
        {
            UserName.Add(userName);
        }

        /// <summary>
        /// Метод RemoveUserName удаляет имя пользователя из коллекции UserName.
        /// Данный метод необходим для статистики.
        /// </summary>
        /// <param name="userName"></param>
        protected internal void RemoveUserName(string userName)
        {
            UserName.Remove(userName);
        }

        /// <summary>
        /// Метод RemoveConnection удаляет клиент из коллекции clients, тем самым отменяя его статус подключения.
        /// </summary>
        /// <param name="id"></param>
        protected internal void RemoveConnection(string id)
        {
            // получаем по id закрытое подключение
            ClientObject client = clients.FirstOrDefault(c => c.Id == id);
            // и удаляем его из списка подключений
            if (client != null)
                clients.Remove(client);
        }


        /// <summary>
        /// Основной метод Listen, который прослушивает все входящие подключения,
        /// создает новый поток, в котором выполняется метод Process объекта ClientObject.
        /// </summary>
        protected internal void Listen()
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.Any, 13000);
                tcpListener.Start();
                Console.WriteLine("Сервер запущен. Ожидание подключений...");

                while (true)
                {
                    TcpClient tcpClient = tcpListener.AcceptTcpClient();

                    ClientObject clientObject = new ClientObject(tcpClient, this);
                    Thread clientThread = new Thread(new ThreadStart(clientObject.Process));
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Disconnect();
            }
        }

       
        /// <summary>
        /// Метод BroadcastMessage отправляет сообщение всем клиентам кроме его отправителя.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="id"></param>
        protected internal void BroadcastMessage(string message, string id)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i].Id != id) // если id клиента не равно id отправляющего
                {
                    clients[i].Stream.Write(data, 0, data.Length); //передача данных
                }
            }
        }

        /// <summary>
        /// Метод MessageToSender отправляет сообщение клиенту, от которого получил сообщение.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="id"></param>
        protected internal void MessageToSender(string message, string id)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            for(int i = 0; i < clients.Count; i++)
            {
                if(clients[i].Id == id) // Если id клиента равно id отправляющего
                {
                    clients[i].Stream.Write(data, 0, data.Length); // Передача данных
                }
            }
        }

        /// <summary>
        /// Метод DataBaseClient выводит список пользователей на текущий момент
        /// </summary>
        /// <returns></returns>
        protected internal string DataBaseClient()
        {
            string dataBaseClient = null;
            
            foreach(var DB in UserName)
            {
                dataBaseClient = dataBaseClient + DB + ",";
            }
            dataBaseClient = dataBaseClient.Remove(dataBaseClient.Length - 1);
            return dataBaseClient;
        }

        /// <summary>
        /// отключение всех клиентов
        /// </summary>
        protected internal void Disconnect()
        {
            tcpListener.Stop(); //остановка сервера

            for (int i = 0; i < clients.Count; i++)
            {
                clients[i].Close(); //отключение клиента
            }
            Environment.Exit(0); //завершение процесса
        }
    }
}
