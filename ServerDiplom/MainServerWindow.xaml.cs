using SimpleTCP;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using Newtonsoft.Json;
using System.Linq;
using System;
using System.Data.Entity;

namespace ServerDiplom
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainServerWindow : Window
    {
        public List<ClientUserTask> clientUserTasks = new List<ClientUserTask>();
        public MainServerWindow()
        {
            InitializeComponent();
            var server = new SimpleTcpServer();

            server.ClientConnected += (sender, e) => Connect(e);
            server.ClientDisconnected += (sender, e) => Disconnect(e);
            server.DataReceived += (sender, e) => DataReceived(e);

            server.Start(new System.Net.IPAddress(new byte[] { 192, 168, 1, 4 }), 5000);
            ListBoxOperations.Items.Add($"Сервер запущен по адресу {server.GetListeningIPs().ToList()[0]}, порт {5000}");
        }

        private void Connect(TcpClient e)
        {
            clientUserTasks.Add(new ClientUserTask() { tcpClient = e, IdTask = 0 });

            Dispatcher.Invoke(() =>
            {
                ListBoxOperations.Items.Add($"Client ({e.Client.RemoteEndPoint}) connected!");
            });
        }
        private void Disconnect(TcpClient e)
        {
            ClientUserTask clientUserTask = clientUserTasks.FirstOrDefault(p => p.tcpClient == e);

            if(clientUserTask.IdTask != 0)
            {
                Task task = DiplomBetaDBEntities.GetContext().Task.First(p => p.Id == clientUserTask.IdTask);
                if (task.Conclusion == null)
                {
                    task.Status = DiplomBetaDBEntities.GetContext().Status.First();
                    DiplomBetaDBEntities.GetContext().SaveChanges();
                }
                else
                {
                    task.Status = DiplomBetaDBEntities.GetContext().Status.First(p => p.Id == 2);
                    DiplomBetaDBEntities.GetContext().SaveChanges();
                }
            }
            clientUserTasks.Remove(clientUserTask);

            Dispatcher.Invoke(() =>
            {
                ListBoxOperations.Items.Add($"Client ({e.Client.RemoteEndPoint}) disconnected!");
            });
        }
        private void DataReceived(Message e)
        {
            var msg = Encoding.UTF8.GetString(e.Data);
            msg = msg.Replace("\u0013", ""); // Отсекаем лишнее
            TCPMessege tCPMessege = JsonConvert.DeserializeObject<TCPMessege>(msg);

            switch (tCPMessege.CodeEntity)
            {
                case 1:
                    {
                        User user = JsonConvert.DeserializeObject<User>(tCPMessege.Entity);
                        user = DiplomBetaDBEntities.GetContext().User.FirstOrDefault(p => p.Password == user.Password && p.Login == user.Login);
                        if (user == null)
                        {
                            tCPMessege = new TCPMessege(0, 1, null);
                            e.Reply(JsonConvert.SerializeObject(tCPMessege));
                            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} неудачно авторизовался; Время операции: {DateTime.Now}.");
                            return;
                        }
                        if(clientUserTasks.Select(p=> p.AutUserId).Contains(user.Id))
                        {
                            tCPMessege = new TCPMessege(-1, 1, null);
                            e.Reply(JsonConvert.SerializeObject(tCPMessege));
                            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} неудачно авторизовался. Пользователь уже авторизован в системе; Время операции: {DateTime.Now}.");
                            return;
                        }
                        ClientUserTask clientUserTask = clientUserTasks.FirstOrDefault(p => p.tcpClient == e.TcpClient);
                        if(clientUserTask != null)
                            clientUserTask.AutUserId = user.Id;
                        tCPMessege = new TCPMessege(1, 1, user);
                        e.Reply(JsonConvert.SerializeObject(tCPMessege));
                        PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} удачно авторизовался под логином {user.Login}; Время операции: {DateTime.Now}.");
                        return;
                    }
                case 2:
                    {
                        switch (tCPMessege.CodeOperation)
                        {
                            case 1:
                                {
                                    PostClients(e);
                                    PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} запросил вытягивание коллекции Client; Время операции: {DateTime.Now}.");
                                    return;
                                }
                            case 3:
                                {
                                    CreateAndPostClients(tCPMessege, e);
                                    return;
                                }
                            case 4:
                                {
                                    UpdateClient(tCPMessege, e);
                                    return;
                                }
                            case 5:
                                {
                                    DeleteClient(tCPMessege, e);
                                    return;
                                }
                        }
                        break;
                    }
                case 3:
                    {
                        switch (tCPMessege.CodeOperation)
                        {
                            case 1:
                                {
                                    PostCarriers(e);
                                    PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} запросил вытягивание коллекции Carrier; Время операции: {DateTime.Now}.");
                                    return;
                                }
                            case 3:
                                {
                                    CreateAndPostCarriers(tCPMessege, e);
                                    return;
                                }
                            case 4:
                                {
                                    UpdateCarriers(tCPMessege, e);
                                    return;
                                }
                            case 5:
                                {
                                    DeleteCarriers(tCPMessege, e);
                                    return;
                                }
                        }
                        break;
                    }
                case 4:
                    {
                        switch (tCPMessege.CodeOperation)
                        {
                            case 1:
                                {
                                    PostTasks(tCPMessege, e);
                                    PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} запросил вытягивание коллекции Task; Время операции: {DateTime.Now}.");
                                    return;
                                }
                            case 3:
                                {
                                    CreateAndPostTask(tCPMessege, e);
                                    return;
                                }
                            case 4:
                                {
                                    UpdateConclTask(tCPMessege, e);
                                    return;
                                }
                            case 5:
                                {
                                    DeleteTask(tCPMessege, e);
                                    return;
                                }
                        }
                        break;
                    }
                case 35:
                    {
                        switch (tCPMessege.CodeOperation)
                        {
                            case 1:
                                {
                                    GetServiceAndCarriers(tCPMessege, e);
                                    PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} запросил вытягивание коллекции Servise&Answer; Время операции: {DateTime.Now}.");
                                    return;
                                }
                            case 3:
                                {
                                    CreateServiceCarrier(tCPMessege, e);
                                    return;
                                }
                            case 4:
                                {
                                    UpdateServiceCarrier(tCPMessege, e);
                                    return;
                                }
                            case 5:
                                {
                                    DeleteServiceCarrier(tCPMessege, e);
                                    return;
                                }
                        }
                        break;


                    }
                case 5:
                    {
                        switch (tCPMessege.CodeOperation)
                        {
                            case 1:
                                {
                                    UpdateTaskPointClient(tCPMessege, e);
                                    return;
                                }
                            case 3:
                                {
                                    CreatePointsList(tCPMessege, e);
                                    return;
                                }
                            case 5:
                                {
                                    DeletePointsList(tCPMessege, e);
                                    return;
                                }
                        }
                        break;
                    }
                case 6:
                    {
                        switch (tCPMessege.CodeOperation)
                        {
                            case 4:
                                {
                                    UpdatePoint(tCPMessege, e);
                                    return;
                                }
                            case 41:
                                {
                                    UpdateClientPoint(tCPMessege, e);
                                    return;
                                }
                        }
                        break;
                    }
                case 7:
                    {
                        switch (tCPMessege.CodeOperation)
                        {
                            case 3:
                                {
                                    CreateTaskService(tCPMessege, e);
                                    return;
                                }
                            case 4:
                                {
                                    CreateTaskCarrier(tCPMessege, e);
                                    return;
                                }
                            case 5:
                                {
                                    DeleteTaskService(tCPMessege, e);
                                    return;
                                }

                        }
                        break;
                    }
                case 8:
                    {
                        switch (tCPMessege.CodeOperation)
                        {
                            case 3:
                                {
                                    AddTransportationCost(tCPMessege, e);
                                    return;
                                }
                            case 4:
                                {
                                    UpdateTC(tCPMessege, e);
                                    return;
                                }
                            case 41:
                                {
                                    UpdateRowAndColums(tCPMessege, e);
                                    return;
                                }
                            case 5:
                                {
                                    DeleteTransportationCost(tCPMessege, e);
                                    return;
                                }
                        }
                        break;
                    }
                case 9:
                    {
                        switch (tCPMessege.CodeOperation)
                        {
                            case 3:
                                {
                                    CreateConstraint(tCPMessege, e);
                                    return;
                                }
                            case 41:
                                {
                                    UpdateConstraintType(tCPMessege, e);
                                    return;
                                }
                            case 42:
                                {
                                    UpdateConstraintVendors(tCPMessege, e);
                                    return;
                                }
                            case 43:
                                {
                                    UpdateConstraintConsumers(tCPMessege, e);
                                    return;
                                }
                            case 44:
                                {
                                    UpdateConstraintCountProduct(tCPMessege, e);
                                    return;
                                }
                            case 5:
                                {
                                    DeleteConstraint(tCPMessege, e);
                                    return;
                                }
                        }
                        break;
                    }
                case 10:
                    {
                        switch (tCPMessege.CodeOperation)
                        {
                            case 1:
                                {
                                    GetTypeConstraint(e);
                                    return;
                                }
                            case 4:
                                {
                                    ChangeStatusTask(tCPMessege, e);
                                    return;
                                }
                        }
                        break;
                    }
                case 11:
                    {
                        switch (tCPMessege.CodeOperation)
                        {
                            case 1:
                                {
                                    CheckStatusTask(tCPMessege, e);
                                    return;
                                }
                        }
                        break;
                    }
            }
        }

        public void PostClients(Message e)
        {
            TCPMessege tCPMessege = new TCPMessege(1, 2, DiplomBetaDBEntities.GetContext().Client.ToList());
            e.Reply(JsonConvert.SerializeObject(tCPMessege));
            return;
        }

        public void PostClient(Message e, Client client)
        {
            TCPMessege tCPMessege = new TCPMessege(1, 2, client);
            e.Reply(JsonConvert.SerializeObject(tCPMessege));
            return;
        }
        private string CheckClient(Client client)
        {
            Client FoundClient = DiplomBetaDBEntities.GetContext().Client.FirstOrDefault(p => (p.CompanyName.Replace(" ","").ToLower() == client.CompanyName.Replace(" ", "").ToLower()
            || p.Address.Replace(" ", "").ToLower() == client.Address.Replace(" ", "").ToLower() || p.Email.Replace(" ", "").ToLower() == client.Email.Replace(" ", "").ToLower()) && p.Id != client.Id);
            string s = "";
            if (FoundClient != null)
            {
                if (client.CompanyName.Replace(" ", "").ToLower() == FoundClient.CompanyName.Replace(" ", "").ToLower())
                {
                    s += "Клиент с заданным наименованием компании уже существует\n";
                }
                if (client.Address.Replace(" ", "").ToLower() == FoundClient.Address.Replace(" ", "").ToLower())
                {
                    s += "Клиент с заданным адресом уже существует\n";
                }
                if (client.Email.Replace(" ", "").ToLower() == FoundClient.Email.Replace(" ", "").ToLower())
                {
                    s += "Клиент с заданной почтой уже существует\n";
                }
                s = s.Trim('\n');
            }
            return s;
        }

        public void CreateAndPostClients(TCPMessege tCPMessege, Message e)
        {
            Client client = JsonConvert.DeserializeObject<Client>(tCPMessege.Entity);
            string ErrorString = CheckClient(client);
            if (ErrorString != "")
            {
                tCPMessege = new TCPMessege(0, 2, ErrorString);
                e.Reply(JsonConvert.SerializeObject(tCPMessege));
                PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} неудачно создал клиента - причина «{ErrorString}»; Время операции: {DateTime.Now}.");
                return;
            }
            client.TypeClient = DiplomBetaDBEntities.GetContext().TypeClient.First(p => p.id == client.TypeId);
            DiplomBetaDBEntities.GetContext().Client.Add(client);
            DiplomBetaDBEntities.GetContext().SaveChanges();
            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} успешно создал Client; Время операции: {DateTime.Now}.");
            PostClient(e,client);
        }

        public void UpdateClient(TCPMessege tCPMessege, Message e)
        {

            List<string> vs = JsonConvert.DeserializeObject<List<string>>(tCPMessege.Entity);
            int idclient = int.Parse(vs[0]), indexColumn = int.Parse(vs[1]);
            Client DBclient = DiplomBetaDBEntities.GetContext().Client.FirstOrDefault(p => p.Id == idclient);
            string ErrorString = "", Value = vs[2];
            if (DBclient == null)
            {
                tCPMessege = new TCPMessege(-1, 0, null);
                e.Reply(JsonConvert.SerializeObject(tCPMessege));
                return;
            }

            try
            {
                switch (indexColumn)
                {
                    case 1:
                        {
                            if(DiplomBetaDBEntities.GetContext().Client.FirstOrDefault(p => p.CompanyName.Replace(" ", "").ToLower() == Value.Replace(" ", "").ToLower() && p.Id != DBclient.Id)!= null)
                            {
                                ErrorString += "Клиент с заданным наименованием компании уже существует";
                                vs[2] = DBclient.CompanyName;
                                break;
                            }
                            DBclient.CompanyName = Value;
                            DiplomBetaDBEntities.GetContext().SaveChanges();
                            break;
                        }
                    case 2:
                        {
                            if (DiplomBetaDBEntities.GetContext().Client.FirstOrDefault(p => p.Address.Replace(" ", "").ToLower() == Value.Replace(" ", "").ToLower() && p.Id != DBclient.Id) != null)
                            {
                                ErrorString += "Клиент с заданным адресом компании уже существует";
                                vs[2] = DBclient.Address;
                                break;
                            }
                            DBclient.Address = Value;
                            DiplomBetaDBEntities.GetContext().SaveChanges();
                            break;
                        }
                    case 3:
                        {
                            if (DiplomBetaDBEntities.GetContext().Client.FirstOrDefault(p => p.Email.Replace(" ", "").ToLower() == Value.Replace(" ", "").ToLower() && p.Id != DBclient.Id) != null)
                            {
                                ErrorString += "Клиент с заданным адресом компании уже существует";
                                vs[2] = DBclient.Email;
                                break;
                            }
                            DBclient.Email = Value;
                            DiplomBetaDBEntities.GetContext().SaveChanges();
                            break;
                        }
                }

                if (ErrorString != "")
                {
                    tCPMessege = new TCPMessege(0, 2, new List<string> { ErrorString,  JsonConvert.SerializeObject(vs)});
                    e.Reply(JsonConvert.SerializeObject(tCPMessege));
                    PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} неудачно создал клиента - причина «{ErrorString}»; Время операции: {DateTime.Now}.");
                    return;
                }

                PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} успешно изменил Client с идентификатором {DBclient.Id}; Время операции: {DateTime.Now}.");

                tCPMessege = new TCPMessege(1, 2, null);
                e.Reply(JsonConvert.SerializeObject(tCPMessege));
            }
            catch (Exception ex)
            {
                tCPMessege = new TCPMessege(0, 2, new List<string> { ex.Message, JsonConvert.SerializeObject(vs) });
                e.Reply(JsonConvert.SerializeObject(tCPMessege));
            }
        }

        public void DeleteClient(TCPMessege tCPMessege, Message e)
        {
            int id = JsonConvert.DeserializeObject<int>(tCPMessege.Entity);
            Client DBclient = DiplomBetaDBEntities.GetContext().Client.FirstOrDefault(p => p.Id == id);
            if (DBclient == null)
                return;
            if(DBclient.Point.Count == 0)
            {
                DiplomBetaDBEntities.GetContext().Client.Remove(DBclient);
                DiplomBetaDBEntities.GetContext().SaveChanges();
                tCPMessege = new TCPMessege(1, 2, null);
            }
            else
            {
                tCPMessege = new TCPMessege(0, 2, "Клиент еще используется в задачах!!! Пожалуйста удалите из всех задач и повторите попытку..");
            }
            e.Reply(JsonConvert.SerializeObject(tCPMessege));
            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} успешно удалил Client {DBclient.CompanyName}; Время операции: {DateTime.Now}.");
        }

        public void PostCarriers(Message e)
        {
            TCPMessege tCPMessege = new TCPMessege(1, 3, DiplomBetaDBEntities.GetContext().Carrier.ToList());
            e.Reply(JsonConvert.SerializeObject(tCPMessege));
            return;
        }

        public void PostCarrier(Message e, Carrier carrier)
        {
            TCPMessege tCPMessege = new TCPMessege(1, 3, carrier);
            e.Reply(JsonConvert.SerializeObject(tCPMessege));
            return;
        }

        public void GetByCarriers(string CN, string Email)
        {

        }

        private string CheckCarrier(Carrier carrier)
        {
            Carrier FoundCarrier = DiplomBetaDBEntities.GetContext().Carrier.FirstOrDefault(p => (p.Name.Replace(" ", "").ToLower() == carrier.Name.Replace(" ", "").ToLower()
            || p.Address.Replace(" ", "").ToLower() == carrier.Address.Replace(" ", "").ToLower() || p.Phone.Replace(" ", "").ToLower() == carrier.Phone.Replace(" ", "").ToLower()
            || p.Email.Replace(" ", "").ToLower() == carrier.Email.Replace(" ", "").ToLower()) && p.Id != carrier.Id);
            string s = "";
            if (FoundCarrier != null)
            {
                if (carrier.Name.Replace(" ", "").ToLower() == FoundCarrier.Name.Replace(" ", "").ToLower())
                {
                    s += "Перевозчик с заданным наименованием уже существует;\n";
                }
                if (carrier.Address.Replace(" ", "").ToLower() == FoundCarrier.Address.Replace(" ", "").ToLower())
                {
                    s += "Перевозчик с заданным адресом уже существует;\n";
                }
                if (carrier.Phone.Replace(" ", "").ToLower() == FoundCarrier.Phone.Replace(" ", "").ToLower())
                {
                    s += "Перевозчик с заданным номером телефона уже существует;\n";
                }
                if (carrier.Email.Replace(" ", "").ToLower() == FoundCarrier.Email.Replace(" ", "").ToLower())
                {
                    s += "Перевозчик с заданной почтой уже существует;\n";
                }
            }
            if(s!= "")
                s = s.Trim('\n');
            return s;
        }

        public void CreateAndPostCarriers(TCPMessege tCPMessege, Message e)
        {
            Carrier carrier = JsonConvert.DeserializeObject<Carrier>(tCPMessege.Entity);
            string errorString = CheckCarrier(carrier);
            if(errorString != "")
            { 
                tCPMessege = new TCPMessege(0, 3, errorString);
                e.Reply(JsonConvert.SerializeObject(tCPMessege));
                PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} неудачно создал перевозчика - причина «{errorString}»; Время операции: {DateTime.Now}.");
                return;
            }

            DiplomBetaDBEntities.GetContext().Carrier.Add(carrier);
            DiplomBetaDBEntities.GetContext().SaveChanges();

            DiplomBetaDBEntities.GetContext().ServiceCarrier.Add(new ServiceCarrier()
            {
                Carrier = carrier,
                Service = DiplomBetaDBEntities.GetContext().Service.First(),
                Cost = 0
            });
            DiplomBetaDBEntities.GetContext().SaveChanges();

            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} успешно создал перевозчика {carrier.Name}; Время операции: {DateTime.Now}.");
            PostCarrier(e, carrier);
        }

        public void UpdateCarriers(TCPMessege tCPMessege, Message e)
        {
            List<string> vs = JsonConvert.DeserializeObject<List<string>>(tCPMessege.Entity);
            int idcarrier = int.Parse(vs[0]), indexColumn = int.Parse(vs[1]);
            Carrier DBcarrier = DiplomBetaDBEntities.GetContext().Carrier.FirstOrDefault(p => p.Id == idcarrier);
            string ErrorString = "", Value = vs[2];
            if(DBcarrier == null)
            {
                tCPMessege = new TCPMessege(-1, 0, null);
                e.Reply(JsonConvert.SerializeObject(tCPMessege));
                return;
            }
            try
            {
                switch (indexColumn)
                {
                    case 1:
                        {
                            if (DiplomBetaDBEntities.GetContext().Carrier.FirstOrDefault(p => p.Name.Replace(" ", "").ToLower() == Value.Replace(" ", "").ToLower() && p.Id != DBcarrier.Id) != null)
                            {
                                ErrorString += "Перевозчик с заданным наименованием компании уже существует";
                                vs[2] = DBcarrier.Name;
                                break;
                            }
                            DBcarrier.Name = Value;
                            DiplomBetaDBEntities.GetContext().SaveChanges();
                            break;
                        }
                    case 2:
                        {
                            if (DiplomBetaDBEntities.GetContext().Carrier.FirstOrDefault(p => p.Address.Replace(" ", "").ToLower() == Value.Replace(" ", "").ToLower() && p.Id != DBcarrier.Id) != null)
                            {
                                ErrorString += "Перевозчик с заданным адресом компании уже существует";
                                vs[2] = DBcarrier.Address;
                                break;
                            }
                            DBcarrier.Address = Value;
                            DiplomBetaDBEntities.GetContext().SaveChanges();
                            break;
                        }
                    case 3:
                        {
                            if (DiplomBetaDBEntities.GetContext().Carrier.FirstOrDefault(p => p.Phone.Replace(" ", "").ToLower() == Value.Replace(" ", "").ToLower() && p.Id != DBcarrier.Id) != null)
                            {
                                ErrorString += "Перевозчик с заданным номером телефона уже существует";
                                vs[2] = DBcarrier.Phone;
                                break;
                            }
                            DBcarrier.Phone = Value;
                            DiplomBetaDBEntities.GetContext().SaveChanges();
                            break;
                        }
                    case 4:
                        {
                            if (DiplomBetaDBEntities.GetContext().Carrier.FirstOrDefault(p => p.Email.Replace(" ", "").ToLower() == Value.Replace(" ", "").ToLower() && p.Id != DBcarrier.Id) != null)
                            {
                                ErrorString += "Перевозчик с заданной почтой компании уже существует";
                                vs[2] = DBcarrier.Email;
                                break;
                            }
                            DBcarrier.Email = Value;
                            DiplomBetaDBEntities.GetContext().SaveChanges();
                            break;
                        }
                }

                if (ErrorString != "")
                {
                    tCPMessege = new TCPMessege(0, 3, new List<string> { ErrorString, JsonConvert.SerializeObject(vs) });
                    e.Reply(JsonConvert.SerializeObject(tCPMessege));
                    PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} неудачно создал клиента - причина «{ErrorString}»; Время операции: {DateTime.Now}.");
                    return;
                }

                PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} успешно изменил Client с идентификатором {DBcarrier.Id}; Время операции: {DateTime.Now}.");

                tCPMessege = new TCPMessege(1, 3, null);
                e.Reply(JsonConvert.SerializeObject(tCPMessege));
            }
            catch (Exception ex)
            {
                tCPMessege = new TCPMessege(0, 3, new List<string> { ex.Message, JsonConvert.SerializeObject(vs) });
                e.Reply(JsonConvert.SerializeObject(tCPMessege));
            }
        }

        public void DeleteCarriers(TCPMessege tCPMessege, Message e)
        {
            int carrierId = JsonConvert.DeserializeObject<int>(tCPMessege.Entity);
            Carrier DBcarrier = DiplomBetaDBEntities.GetContext().Carrier.FirstOrDefault(p => p.Id == carrierId);
            if (DBcarrier == null)
                return;
            if(DBcarrier.Task.Count != 0)
            {
                tCPMessege = new TCPMessege(0, 3, null);
                e.Reply(JsonConvert.SerializeObject(tCPMessege));
                PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} неудачно удалил Carrier {DBcarrier.Name} причина - перевозчик участвует в задачах; Время операции: {DateTime.Now}.");
                return;
            }
            DiplomBetaDBEntities.GetContext().Carrier.Remove(DBcarrier);
            DiplomBetaDBEntities.GetContext().SaveChanges();
            tCPMessege = new TCPMessege(1, 3, null);
            e.Reply(JsonConvert.SerializeObject(tCPMessege));
            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} успешно удалил Carrier {DBcarrier.Name}; Время операции: {DateTime.Now}.");
        }

        public void DeleteTask(TCPMessege tCPMessege, Message e)
        {
            int taskId = JsonConvert.DeserializeObject<int>(tCPMessege.Entity);
            Task DBtask = DiplomBetaDBEntities.GetContext().Task.FirstOrDefault(p => p.Id == taskId);
            if (DBtask == null)
                return;
            DiplomBetaDBEntities.GetContext().Task.Remove(DBtask);
            DiplomBetaDBEntities.GetContext().SaveChanges();

            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} успешно удалил Task; Время операции: {DateTime.Now}.");
        }

        private void PrintConsole(string s)
        {
            Dispatcher.Invoke(() =>
            {
                ListBoxOperations.Items.Add(s);
            });
        }

        private void GetServiceAndCarriers(TCPMessege tCPMessege, Message e)
        {
            JsonSerializerSettings JSS = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            };

            List<Service> services = DiplomBetaDBEntities.GetContext().Service.ToList();
            List<Carrier> carriers = DiplomBetaDBEntities.GetContext().Carrier.ToList();

            List<string> AnswerEntityesString = new List<string>() { JsonConvert.SerializeObject(services, Formatting.Indented, JSS), JsonConvert.SerializeObject(carriers, Formatting.Indented, JSS) };

            tCPMessege = new TCPMessege(1, 35, AnswerEntityesString);
            e.Reply(JsonConvert.SerializeObject(tCPMessege));
        }

        private void CreateServiceCarrier(TCPMessege tCPMessege, Message e)
        {
            List<int> vs = JsonConvert.DeserializeObject<List<int>>(tCPMessege.Entity);
            int f = vs[0], s = vs[1];

            if (DiplomBetaDBEntities.GetContext().ServiceCarrier.FirstOrDefault(p => p.IdService == f && p.IdCarrier == s) != null)
            {
                return;
            }

            Service service = DiplomBetaDBEntities.GetContext().Service.First(p => p.Id == f);
            Carrier carrier = DiplomBetaDBEntities.GetContext().Carrier.FirstOrDefault(p => p.Id == s);
            if(carrier == null)
            {
                tCPMessege = new TCPMessege(0, 0, null);
                e.Reply(JsonConvert.SerializeObject(tCPMessege));
                return;
            }

            ServiceCarrier serviceCarrier = new ServiceCarrier() { Carrier = carrier, Service = service, Cost = 0 };
            DiplomBetaDBEntities.GetContext().ServiceCarrier.Add(serviceCarrier);
            DiplomBetaDBEntities.GetContext().SaveChanges();
            tCPMessege = new TCPMessege(1, 0, null);
            e.Reply(JsonConvert.SerializeObject(tCPMessege));
            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} добавил услугу {service.Name}, перевозчику {carrier.Name}; Время операции: {DateTime.Now}.");
        }

        private void UpdateServiceCarrier(TCPMessege tCPMessege, Message e)
        {
            List<int> vs = JsonConvert.DeserializeObject<List<int>>(tCPMessege.Entity);
            int f = vs[0], s = vs[1], t = vs[2];

            ServiceCarrier serviceCarrier = DiplomBetaDBEntities.GetContext().ServiceCarrier.First(p => p.IdService == f && p.IdCarrier == s);
            if(serviceCarrier == null)
            {
                tCPMessege = new TCPMessege(0, 0, null);
                e.Reply(JsonConvert.SerializeObject(tCPMessege));
                return;
            }
            serviceCarrier.Cost = t;
            DiplomBetaDBEntities.GetContext().SaveChanges();
            tCPMessege = new TCPMessege(1, 0, null);
            e.Reply(JsonConvert.SerializeObject(tCPMessege));
            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} изменил стоимость услуги {serviceCarrier.Service.Name}, перевозчика {serviceCarrier.Carrier.Name}; Время операции: {DateTime.Now}.");
        }

        private void DeleteServiceCarrier(TCPMessege tCPMessege, Message e)
        {
            List<int> vs = JsonConvert.DeserializeObject<List<int>>(tCPMessege.Entity);
            int f = vs[0], s = vs[1];

            ServiceCarrier serviceCarrier = DiplomBetaDBEntities.GetContext().ServiceCarrier.FirstOrDefault(p => p.IdService == f && p.IdCarrier == s);
            if (serviceCarrier == null)
            {
                tCPMessege = new TCPMessege(0, 0, null);
                e.Reply(JsonConvert.SerializeObject(tCPMessege));
                return;
            }

            DiplomBetaDBEntities.GetContext().ServiceCarrier.Remove(serviceCarrier);
            DiplomBetaDBEntities.GetContext().SaveChanges();
            tCPMessege = new TCPMessege(1, 0, null);
            e.Reply(JsonConvert.SerializeObject(tCPMessege));
            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} удалил услугу {serviceCarrier.Service.Name}, перевозчика {serviceCarrier.Carrier.Name}; Время операции: {DateTime.Now}.");
        }

        private void PostTasks(TCPMessege tCPMessege, Message e)
        {
            tCPMessege = new TCPMessege(1, 4, DiplomBetaDBEntities.GetContext().Task);
            e.Reply(JsonConvert.SerializeObject(tCPMessege));
        }

        private void PostTask(Message e, Task task)
        {
            TCPMessege tCPMessege = new TCPMessege(1, 4, task);
            e.Reply(JsonConvert.SerializeObject(tCPMessege));
        }

        public void CreateAndPostTask(TCPMessege tCPMessege, Message e)
        {
            try
            {
                int UserId = JsonConvert.DeserializeObject<int>(tCPMessege.Entity);
                User user = DiplomBetaDBEntities.GetContext().User.First(p => p.Id == UserId);
                Status status = DiplomBetaDBEntities.GetContext().Status.ToList().Last();
                Task task = new Task()
                {
                    User = user,
                    Status = status,
                    Point = new List<Point>(),
                    TransportationCost = new List<TransportationCost>(),
                    Constraint = new List<Constraint>(),
                    CountColumn = 0,
                    CountRow = 0
                };
                task.Service.Add(DiplomBetaDBEntities.GetContext().Service.First());
                DiplomBetaDBEntities.GetContext().Task.Add(task);
                DiplomBetaDBEntities.GetContext().SaveChanges();
                PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} успешно создал Task; Время операции: {DateTime.Now}.");
                PostTask(e, task);
            }
            catch(Exception ex)
            {
                tCPMessege = new TCPMessege(0, 4, ex.Message);
                e.Reply(JsonConvert.SerializeObject(tCPMessege));
            }
        }

        public void UpdateTaskPointClient(TCPMessege tCPMessege, Message e)
        {
            List<int> vs = JsonConvert.DeserializeObject<List<int>>(tCPMessege.Entity);
            int idTask = vs[0], idDeletedClient = vs[1], IdNewClient = vs[2];

            Task task = DiplomBetaDBEntities.GetContext().Task.First(p => p.Id == idTask);
            Client client = DiplomBetaDBEntities.GetContext().Client.First(p => p.Id == IdNewClient);

            try
            {
                foreach (Point point in task.Point)
                {
                    if (point.Client.Id == idDeletedClient)
                    {
                        point.Client = client;
                    }
                }
                DiplomBetaDBEntities.GetContext().SaveChanges();
                tCPMessege = new TCPMessege(1, 5, task);
                e.Reply(JsonConvert.SerializeObject(tCPMessege));
                PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} обновил состав клиентов задачи {task.Id}; Время операции: {DateTime.Now}.");
            }
            catch(Exception ex)
            {
                tCPMessege = new TCPMessege(0, 5, ex.Message);
                e.Reply(JsonConvert.SerializeObject(tCPMessege));
                PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} неудачно обновил состав клиентов задачи {task.Id}, причина - {ex.Message}; Время операции: {DateTime.Now}.");
            }
        }

        public void CreatePointsList(TCPMessege tCPMessege, Message e)
        {
            List<int> vs = JsonConvert.DeserializeObject<List<int>>(tCPMessege.Entity);
            int idTask = vs[0], idClient = vs[1], countIteration = vs[2];

            Task task = DiplomBetaDBEntities.GetContext().Task.First(p => p.Id == idTask);
            Client client = DiplomBetaDBEntities.GetContext().Client.First(p => p.Id == idClient);
            string pointName;
            if (client.TypeId == 1)
                pointName = "Магазин ";
            else
                pointName = "Склад ";
            try
            {
                for (int i = 0; i < countIteration; i++)
                {
                    int pos = task.Point.Where(p => p.Client.TypeId == client.TypeId).ToList().Count + 1;
                    DiplomBetaDBEntities.GetContext().Point.Add(new Point() { Task = task, Address = "", Client = client, Name = pointName + pos, Position = pos, ProductCount = 0 });
                }
                DiplomBetaDBEntities.GetContext().SaveChanges();

                tCPMessege = new TCPMessege(1, 5, task);
                e.Reply(JsonConvert.SerializeObject(tCPMessege));
                PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} обновил число пунктов задачи {task.Id}; Время операции: {DateTime.Now}.");
            }
            catch (Exception ex)
            {
                tCPMessege = new TCPMessege(0, 5, ex.Message);
                e.Reply(JsonConvert.SerializeObject(tCPMessege));
                PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} неудачно число пунктов задачи {task.Id}, причина - {ex.Message}; Время операции: {DateTime.Now}.");
            }
        }

        public void DeletePointsList(TCPMessege tCPMessege, Message e)
        {
            List<int> vs = JsonConvert.DeserializeObject<List<int>>(tCPMessege.Entity);
            int idTask = vs[0], idClient = vs[1], countIteration = vs[2];

            Task task = DiplomBetaDBEntities.GetContext().Task.First(p => p.Id == idTask);
            Client client = DiplomBetaDBEntities.GetContext().Client.First(p => p.Id == idClient);
            try
            {
                for (int i = 0; i > countIteration; i--) // Удаление точек
                {
                    DiplomBetaDBEntities.GetContext().Point.Remove(task.Point.Last(p => p.Client.TypeId == client.TypeId));
                }

                DiplomBetaDBEntities.GetContext().SaveChanges();

                tCPMessege = new TCPMessege(1, 5, task);
                e.Reply(JsonConvert.SerializeObject(tCPMessege));
                PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} обновил число пунктов задачи {task.Id}; Время операции: {DateTime.Now}.");
            }
            catch (Exception ex)
            {
                tCPMessege = new TCPMessege(0, 5, ex.Message);
                e.Reply(JsonConvert.SerializeObject(tCPMessege));
                PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} неудачно число пунктов задачи {task.Id}, причина - {ex.Message}; Время операции: {DateTime.Now}.");
            }
        }

        public void UpdateClientPoint(TCPMessege tCPMessege, Message e)
        {
            List<int> vs = JsonConvert.DeserializeObject<List<int>>(tCPMessege.Entity);
            int idPoint = vs[0], idClient = vs[1];

            try
            {
                Point point = DiplomBetaDBEntities.GetContext().Point.First(p => p.Id == idPoint);
                Client client = DiplomBetaDBEntities.GetContext().Client.FirstOrDefault(p => p.Id == idClient);
                if (client != null)
                {
                    point.Client = client;
                    DiplomBetaDBEntities.GetContext().SaveChanges();
                    PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} успешно изменил у пунка {point.Name} клиента на {client.CompanyName}; Время операции: {DateTime.Now}.");
                    tCPMessege = new TCPMessege(1, 5, null);
                }
                else
                {
                    tCPMessege = new TCPMessege(0, 5, "Данный клиент был удален из системы!!!");
                }
            }
            catch(Exception ex)
            {
                tCPMessege = new TCPMessege(-1, 5, ex.Message);
            }
            e.Reply(JsonConvert.SerializeObject(tCPMessege));
        }

        public void UpdatePoint(TCPMessege tCPMessege, Message e)
        {
            List<string> vs = JsonConvert.DeserializeObject<List<string>>(tCPMessege.Entity);
            int idPoint = int.Parse(vs[0]), idAttribyte = int.Parse(vs[1]);
            string value = vs[2];

            Point DBPoint = DiplomBetaDBEntities.GetContext().Point.First(p => p.Id == idPoint);
            string ErrorString = "";
            switch (idAttribyte)
            {
                case 2:
                    {
                        if (DBPoint.Task.Point.FirstOrDefault(p => p.Name.Replace(" ", "").ToLower() == value.Replace(" ", "").ToLower() && p.Id != DBPoint.Id) != null)
                        {
                            ErrorString += "Данное наименование уже существует в рамках заявки";
                            vs[2] = DBPoint.Name;
                            break;
                        }
                        DBPoint.Name = value;
                        DiplomBetaDBEntities.GetContext().SaveChanges();
                        break;
                    }
                case 3:
                    {
                        if (DBPoint.Task.Point.FirstOrDefault(p => p.Address.Replace(" ", "").ToLower() == value.Replace(" ", "").ToLower() && p.Id != DBPoint.Id) != null)
                        {
                            ErrorString += "Данный адрес уже существует в рамках заявки";
                            vs[2] = DBPoint.Address;
                            break;
                        }
                        DBPoint.Address = value;
                        DiplomBetaDBEntities.GetContext().SaveChanges();
                        break;
                    }
                case 4:
                    {
                        DBPoint.ProductCount = int.Parse(value);
                        DiplomBetaDBEntities.GetContext().SaveChanges();
                        break;
                    }
            }
            if (ErrorString != "")
            {
                tCPMessege = new TCPMessege(0, 0, new List<string> { ErrorString, JsonConvert.SerializeObject(vs) });
                e.Reply(JsonConvert.SerializeObject(tCPMessege));
                PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} неудачно изменил пункт клиента - причина «{ErrorString}»; Время операции: {DateTime.Now}.");
                return;
            }

            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} успешно изменил пункт {DBPoint.Id}; Время операции: {DateTime.Now}.");

            tCPMessege = new TCPMessege(1, 3, null);
            e.Reply(JsonConvert.SerializeObject(tCPMessege));
        }

        private void CreateTaskService(TCPMessege tCPMessege, Message e)
        {
            List<int> vs = JsonConvert.DeserializeObject<List<int>>(tCPMessege.Entity);
            int IdTask = vs[0], IdService = vs[1];

            Task task = DiplomBetaDBEntities.GetContext().Task.First(p => p.Id == IdTask);
            Service service = DiplomBetaDBEntities.GetContext().Service.First(p => p.Id == IdService);

            task.Service.Add(service);
            DiplomBetaDBEntities.GetContext().SaveChanges();
            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} добавил услугу {service.Name}, задаче {task.Id}; Время операции: {DateTime.Now}.");
        }

        private void DeleteTaskService(TCPMessege tCPMessege, Message e)
        {
            List<int> vs = JsonConvert.DeserializeObject<List<int>>(tCPMessege.Entity);
            int IdTask = vs[0], IdService = vs[1];

            Task task = DiplomBetaDBEntities.GetContext().Task.First(p => p.Id == IdTask);
            Service service = DiplomBetaDBEntities.GetContext().Service.First(p => p.Id == IdService);

            task.Service.Remove(service);
            DiplomBetaDBEntities.GetContext().SaveChanges();
            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} добавил услугу {service.Name}, задаче {task.Id}; Время операции: {DateTime.Now}.");
        }

        private void CreateTaskCarrier(TCPMessege tCPMessege, Message e)
        {
            List<int> vs = JsonConvert.DeserializeObject<List<int>>(tCPMessege.Entity);
            int IdTask = vs[0], IdCarrier = vs[1];

            Task task = DiplomBetaDBEntities.GetContext().Task.First(p => p.Id == IdTask);
            Carrier carrier = DiplomBetaDBEntities.GetContext().Carrier.FirstOrDefault(p => p.Id == IdCarrier);
            if(carrier == null)
            {
                tCPMessege = new TCPMessege(0, 0, null);
                e.Reply(JsonConvert.SerializeObject(tCPMessege));
                return;
            }
            task.Carrier = carrier;
            DiplomBetaDBEntities.GetContext().SaveChanges();

            tCPMessege = new TCPMessege(1, 0, null);
            e.Reply(JsonConvert.SerializeObject(tCPMessege));
            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} добавил перевозчика {carrier.Name}, задаче {task.Id}; Время операции: {DateTime.Now}.");
        }

        private void UpdateRowAndColums(TCPMessege tCPMessege, Message e)
        {
            List<int> vs = JsonConvert.DeserializeObject<List<int>>(tCPMessege.Entity);
            int IdTask = vs[0], countRow = vs[1], countColumn = vs[2];

            Task task = DiplomBetaDBEntities.GetContext().Task.First(p => p.Id == IdTask);
            task.CountRow = countRow;
            task.CountColumn = countColumn;
            DiplomBetaDBEntities.GetContext().SaveChanges();
            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} изменил последнее состояние строк - столбцов, задачи {task.Id}; Время операции: {DateTime.Now}.");
        }

        private void DeleteTransportationCost(TCPMessege tCPMessege, Message e)
        {
            List<string> vs = JsonConvert.DeserializeObject<List<string>>(tCPMessege.Entity);
            int IdTask = int.Parse(vs[0]);
            Task task = DiplomBetaDBEntities.GetContext().Task.First(p => p.Id == IdTask);
            List<TransportationCost> DeleteTC = JsonConvert.DeserializeObject<List<TransportationCost>>(vs[1]);
            foreach(TransportationCost transportationCost in DeleteTC)
            {
                DiplomBetaDBEntities.GetContext().TransportationCost.Remove(DiplomBetaDBEntities.GetContext().TransportationCost.First(p => p.Id == transportationCost.Id));
            }
            DiplomBetaDBEntities.GetContext().SaveChanges();
            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} удалил список тарифов, задачи {task.Id}; Время операции: {DateTime.Now}.");
        }

        private void AddTransportationCost(TCPMessege tCPMessege, Message e)
        {
            List<string> vs = JsonConvert.DeserializeObject<List<string>>(tCPMessege.Entity);
            int IdTask = int.Parse(vs[0]);
            Task task = DiplomBetaDBEntities.GetContext().Task.First(p => p.Id == IdTask);
            List<TransportationCost> AddTC = JsonConvert.DeserializeObject<List<TransportationCost>>(vs[1]);
            foreach (TransportationCost transportationCost in AddTC)
            {
                transportationCost.Task = task;
                DiplomBetaDBEntities.GetContext().TransportationCost.Add(transportationCost);
                DiplomBetaDBEntities.GetContext().SaveChanges();
            }
            AddTC = DiplomBetaDBEntities.GetContext().TransportationCost.Where(P => P.IdTask == IdTask).ToList();
            tCPMessege.Entity = JsonConvert.SerializeObject(AddTC);
            e.Reply(JsonConvert.SerializeObject(tCPMessege));
            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} добавил список тарифов, задачи {task.Id}; Время операции: {DateTime.Now}.");
        }

        private void UpdateTC(TCPMessege tCPMessege, Message e)
        {
            List<int> vs = JsonConvert.DeserializeObject<List<int>>(tCPMessege.Entity);
            int IdTC = vs[0], cost = vs[1];

            TransportationCost transportationCost = DiplomBetaDBEntities.GetContext().TransportationCost.First(p => p.Id == IdTC);
            transportationCost.Value = cost;
            DiplomBetaDBEntities.GetContext().SaveChanges();
            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} изменил стоимость тарифа {transportationCost.Id}; Время операции: {DateTime.Now}.");
        }

        public void CreateConstraint(TCPMessege tCPMessege, Message e)
        {
            int idTask = JsonConvert.DeserializeObject<int>(tCPMessege.Entity);
            Task task = DiplomBetaDBEntities.GetContext().Task.First(p => p.Id == idTask);
            Constraint constraint = new Constraint() { Task = task, TypeConstraint = DiplomBetaDBEntities.GetContext().TypeConstraint.First(), ProductCount = 0 };
            DiplomBetaDBEntities.GetContext().Constraint.Add(constraint);
            DiplomBetaDBEntities.GetContext().SaveChanges();
            tCPMessege = new TCPMessege(1, 9, constraint);
            e.Reply(JsonConvert.SerializeObject(tCPMessege));
            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} успешно создал ограничение; Время операции: {DateTime.Now}.");
        }

        public void UpdateConstraintType(TCPMessege tCPMessege, Message e)
        {
            List<int> vs = JsonConvert.DeserializeObject<List<int>>(tCPMessege.Entity);
            int idConstraint = vs[0], idType = vs[1];
            Constraint constraint = DiplomBetaDBEntities.GetContext().Constraint.First(p => p.Id == idConstraint);
            TypeConstraint typeConstraint = DiplomBetaDBEntities.GetContext().TypeConstraint.First(p => p.Id == idType);
            constraint.TypeConstraint = typeConstraint;
            DiplomBetaDBEntities.GetContext().SaveChanges();
            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} успешно изменил на тип ограничения {typeConstraint.Name} у ограничения под номером {constraint.Id}; Время операции: {DateTime.Now}.");
        }

        public void UpdateConstraintVendors(TCPMessege tCPMessege, Message e)
        {
            List<string> vs = JsonConvert.DeserializeObject<List<string>>(tCPMessege.Entity);
            int IdConstraint = int.Parse(vs[0]), IdPosition = int.Parse(vs[1]);
            Constraint constraint = DiplomBetaDBEntities.GetContext().Constraint.First(p => p.Id == IdConstraint);
            if (constraint.IdPoints == null)
            {
                constraint.IdPoints = IdPosition + "&" + "-1";
            }
            else
            {
                List<string> ListPoints = constraint.IdPoints.Split('&').ToList();
                ListPoints[0] = IdPosition.ToString();
                constraint.IdPoints = ListPoints[0] + "&" + ListPoints[1];
            }
            DiplomBetaDBEntities.GetContext().SaveChanges();
            tCPMessege.Entity = constraint.IdPoints;
            e.Reply(JsonConvert.SerializeObject(tCPMessege));
            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} успешно изменил пункт поставки в ограничении под номером {constraint.Id}; Время операции: {DateTime.Now}.");
        }

        public void UpdateConstraintConsumers(TCPMessege tCPMessege, Message e)
        {
            List<string> vs = JsonConvert.DeserializeObject<List<string>>(tCPMessege.Entity);
            int IdConstraint = int.Parse(vs[0]), IdPosition = int.Parse(vs[1]);
            Constraint constraint = DiplomBetaDBEntities.GetContext().Constraint.First(p => p.Id == IdConstraint);
            if (constraint.IdPoints == null)
            {
                constraint.IdPoints = "-1" + "&" + IdPosition;
            }
            else
            {
                List<string> ListPoints = constraint.IdPoints.Split('&').ToList();
                ListPoints[1] = IdPosition.ToString();
                constraint.IdPoints = ListPoints[0] + "&" + ListPoints[1];
            }
            DiplomBetaDBEntities.GetContext().SaveChanges();
            tCPMessege.Entity = constraint.IdPoints;
            e.Reply(JsonConvert.SerializeObject(tCPMessege));
            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} успешно изменил пункт поставки в ограничении под номером {constraint.Id}; Время операции: {DateTime.Now}.");
        }
        public void UpdateConstraintCountProduct(TCPMessege tCPMessege, Message e)
        {
            List<int> vs = JsonConvert.DeserializeObject<List<int>>(tCPMessege.Entity);
            int idConstraint = vs[0];
            Constraint constraint = DiplomBetaDBEntities.GetContext().Constraint.First(p => p.Id == idConstraint);
            constraint.ProductCount = vs[1];
            DiplomBetaDBEntities.GetContext().SaveChanges();
            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} успешно изменил число продукции у ограничения под номером {constraint.Id}; Время операции: {DateTime.Now}.");
        }

        public void DeleteConstraint(TCPMessege tCPMessege, Message e)
        {
            int id = JsonConvert.DeserializeObject<int>(tCPMessege.Entity);
            Constraint constraint = DiplomBetaDBEntities.GetContext().Constraint.First(p => p.Id == id);
            DiplomBetaDBEntities.GetContext().Constraint.Remove(constraint);
            DiplomBetaDBEntities.GetContext().SaveChanges();
            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} успешно удалил ограничение; Время операции: {DateTime.Now}.");
        }

        private void GetTypeConstraint(Message e)
        {
            TCPMessege tCPMessege = new TCPMessege(1, 10, DiplomBetaDBEntities.GetContext().TypeConstraint.ToList());
            e.Reply(JsonConvert.SerializeObject(tCPMessege));
            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} запросил вытягивание коллекции TypeConstrain; Время операции: {DateTime.Now}.");
        }

        private void UpdateConclTask(TCPMessege tCPMessege, Message e)
        {
            List<string> vs = JsonConvert.DeserializeObject<List<string>>(tCPMessege.Entity);
            int idTask = int.Parse(vs[0]), sum = int.Parse(vs[2]);
            Task task = DiplomBetaDBEntities.GetContext().Task.First(p => p.Id == idTask);
            task.Conclusion = vs[1];
            task.Cost = sum;
            task.Status = DiplomBetaDBEntities.GetContext().Status.First(p => p.Id == 2);
            DiplomBetaDBEntities.GetContext().SaveChanges();
            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} сформировал вывод для задачи {task.Id}; Время операции: {DateTime.Now}.");
        }

        public void ChangeStatusTask(TCPMessege tCPMessege, Message e)
        {
            List<int> vs = JsonConvert.DeserializeObject<List<int>>(tCPMessege.Entity);
            int IdTask = vs[0], IdStatus = vs[1];
            Task task = DiplomBetaDBEntities.GetContext().Task.First(p => p.Id == IdTask);
            if(clientUserTasks.Where(p => p.tcpClient != e.TcpClient).Select(p => p.IdTask).Contains(IdTask))
            {
                tCPMessege = new TCPMessege(0, 0, task.Status);
                e.Reply(JsonConvert.SerializeObject(tCPMessege));
                return;
            }
            if(IdStatus == 3)
            {
                ClientUserTask clientUserTask = clientUserTasks.First(p => p.tcpClient == e.TcpClient);
                clientUserTask.IdTask = IdTask;
            }
            else
            {
                ClientUserTask clientUserTask = clientUserTasks.First(p => p.tcpClient == e.TcpClient);
                clientUserTask.IdTask = 0;
            }
            task.Status = DiplomBetaDBEntities.GetContext().Status.First(p => p.Id == IdStatus);
            DiplomBetaDBEntities.GetContext().SaveChanges();
            tCPMessege = new TCPMessege(1, 0, task.Status);
            e.Reply(JsonConvert.SerializeObject(tCPMessege));
            PrintConsole($"Пользователь {e.TcpClient.Client.RemoteEndPoint} успешно изменил статус задачи {task.Id}; Время операции: {DateTime.Now}.");
        }

        public void CheckStatusTask(TCPMessege tCPMessege, Message e)
        {
            int IdTask = JsonConvert.DeserializeObject<int>(tCPMessege.Entity);
            Task task = DiplomBetaDBEntities.GetContext().Task.FirstOrDefault(p => p.Id == IdTask);
            if(task != null)
            {
                if (clientUserTasks.Where(p => p.tcpClient != e.TcpClient).Select(p => p.IdTask).Contains(IdTask))
                {
                    tCPMessege = new TCPMessege(0, 0, null);
                    e.Reply(JsonConvert.SerializeObject(tCPMessege));
                    return;
                }
            }
            tCPMessege = new TCPMessege(1, 0, null);
            e.Reply(JsonConvert.SerializeObject(tCPMessege));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Status status1 = DiplomBetaDBEntities.GetContext().Status.First();
            Status status2 = DiplomBetaDBEntities.GetContext().Status.First(p => p.Id == 2);
            List<Task> tasks = DiplomBetaDBEntities.GetContext().Task.Where(p => p.StatusId == 3).ToList();
            for (int i = 0; i < tasks.Count; i++ )
            {
                if (tasks[i].Conclusion == null)
                {
                    tasks[i].Status = status1;
                    DiplomBetaDBEntities.GetContext().SaveChanges();
                }
                else
                {
                    tasks[i].Status = status2;
                    DiplomBetaDBEntities.GetContext().SaveChanges();
                }
            }
        }
    }
}
