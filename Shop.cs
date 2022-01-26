using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace AsyncProgram_1
{
    class Shop
    {
        private string Title { get; set; } //название магазина
        private int peopleCount { get; set; } //кол-во находящихся людей
        private Cashier[] cashiers; //коллекция касс
        public double Balance
        {
            get
            {
                double balance = 0;
                for (int i = 0; i < cashiers.Length; i++) balance += cashiers[i].Balance; //считаем баланс всех касс
                return balance;
            }
            set { }
        } //подсчёт баланса магазина

        public Shop(string title,int people,sbyte kassaCount)
        {
            Title = title;
            peopleCount= people;
            cashiers = new Cashier[kassaCount];
        }
        public void Start()
        {
            StartService(); //инициализация касс
            Thread[] threads = new Thread[cashiers.Length]; //массив потоков для асинхронного выполнения

            for (int i = 0; i < cashiers.Length; i++)
            {
                threads[i] = new Thread(cashiers[i].ServiceClient);
                threads[i].Start();
            }
        }
        private void StartService()
        {
            int counts = peopleCount / cashiers.Length; //распределение людей на кассы
            for (int i = 0; i < cashiers.Length; i++) 
            {
                cashiers[i] = new Cashier(counts, i + 1); //передаём кол-во людей на кассу
                Thread.Sleep(30);
            }
        } //запуск обслуживания
    } //класс магазина

    class Cashier
    {
        public int Id { get; set; } //айди кассы
        private string Name { get; set; } //имя кассира
        private Client[] clients; //список клиентов на кассе
        private WriteInfo Log; //объект для записи в файл
        private Stopwatch watch = new Stopwatch();

        public double Balance { get; set; } //общее кол-во денег в кассе

        public Cashier(int count,int Id)
        {
            Init(count);
            this.Id = Id;
            Log = new WriteInfo(Id, Name);
        } //конструктор класса

        public void ServiceClient()
        {
            Console.WriteLine("Касса №{0} начала обслуживание в потоке {1}", Id, Thread.CurrentThread.ManagedThreadId);
            watch.Start();

            InitializeClients();
            ClientsPay();

            watch.Stop();
            Console.WriteLine("Касса №{0} закончила обслуживание в потоке {1} за {2}", Id, Thread.CurrentThread.ManagedThreadId, watch.Elapsed);
        } //асинхронное обслуживание клиентов

        private void Init(int count)
        {
            clients = new Client[count];
            string path = AppContext.BaseDirectory + "NameClients.txt";
            string[] counts = File.ReadAllLines(path);
            StreamReader reader = new StreamReader(path, Encoding.UTF8);
            Name = counts[new Random().Next(1, counts.Length)]; //случайное имя
            reader.Close();
        } //инициализация кассы
        private void ClientsPay()
        {
            for (int i = 0; i < clients.Length; i++)
            {
                Log.WriteSeparator('-');
                Log.Write("Клиент ", clients[i].name + ", Баланс " + clients[i].balance); //записываем имя и баланс клиента
                for (int j = 0; j < clients[i].products.Count; j++)
                    Log.Write("Покупка", clients[i].products[j]); //запись в лог файл
            } //запись данных о клиенте

            Log.WriteSeparator('+'); 

            for (int i = 0; i < clients.Length; i++) 
            {
                Balance += clients[i].Pay();
                Log.Write("Оплата " + clients[i].name, "Сдача: " + clients[i].balance.ToString());
            } //запись данных об оплате

            Log.WriteSeparator('$');
            Log.Write("Баланс кассы:", Balance.ToString());
        } //оплата клиентов
        private void InitializeClients()
        {
            for (int i = 0; i < clients.Length; i++)
            {
                clients[i] = new Client();
                clients[i].Initialize();
                Thread.Sleep(50); //имитация покупок клиентов (нужно время для рандомайзера)
            }
        } //инициализация клиентов
    } //класс кассы

    class Client
    {
        public string name { get; set; } //имя и фамилия клиента
        public double balance { get; set; } //баланс клиента
        public List<string> products = new List<string>(); //набранные продукты

        private readonly Random r = new Random(); //рандомайзер

        public Client() { }

        public double Pay()
        {
            double sum = SumAllProducts(); //сумма покупок

            if (sum <= balance)  //если денег хватает то покупает
            {
                balance -= sum;
                return sum;
            } else
            {
                DeleteProducts(Math.Abs(balance - sum)); //удаляем дорогой товар
                return Pay(); //повторное выполнение метода
            }
        } //оплата клиента
        public void Initialize()
        {
            InitializeName();
            InitializeProducts();
            SortProducts();
        } //инициализация клиента

        private double SumAllProducts()
        {
            double sum = 0;
            for (int i = 0; i < products.Count; i++)
            {
                sum += Convert.ToDouble(products[i].Split(':')[1]); //складываем цены набранных продуктов
            }
            return sum;
        } //считает сумму всех товаров
        private void SortProducts()
        {
            for (int j = 0; j < products.Count; j++)
            {
                for (int i = 0; i < products.Count - 1; i++)
                {
                    string buf;
                    double a = Convert.ToDouble(products[i].Split(':')[1]);
                    double b = Convert.ToDouble(products[i + 1].Split(':')[1]);

                    if (a > b)
                    {
                        buf = products[i + 1];
                        products[i + 1] = products[i];
                        products[i] = buf;
                    }
                }
            }
        } //сортировка продуктов по возрастанию цены
        private void DeleteProducts(double sum)
        {
            double maxCost = Convert.ToDouble(products[products.Count-1].Split(':')[1]);
            //если недостающая сумма находится в диапазоне цен продуктов
            if (sum <= maxCost) 
            {
                for (int i = 0; i < products.Count; i++)
                {
                    //ищет ближайший продукт для недостающей цены
                    if (sum <= Convert.ToDouble(products[i].Split(':')[1])) //если недостаток меньше цены продукта
                    {
                        //Log.Write(name+" удалил товар", products[i]);
                        products.Remove(products[i]);
                        return;
                    }
                }
            } 
            //если недостающая цена больше чем самый дорогой товар клиента
            else
            {
                //Log.Write(name + " удалил товар", products[products.Count-1]);
                products.Remove(products[products.Count - 1]); //удаляем самый дорогой товар
            }

        } //удаляет лишние продукты
        private void InitializeProducts()
        {
            string path = AppContext.BaseDirectory + "Products.txt";
            StreamReader reader = new StreamReader(path, Encoding.UTF8);

            string[] countProducts = File.ReadAllLines(path); //список строк файла
            int count = r.Next(1, countProducts.Length / 5); //случ.кол-во продуктов
            balance = count * 150; //случ. баланс

            //выбираем случайные продукты и добавляем в список
            for (int i = 0; i < count; i++) products.Add(countProducts[r.Next(1, countProducts.Length)]);

            reader.Close();
        } //случайное заполнение списка продуктов и нач. баланс
        private void InitializeName()
        {
            string path = AppContext.BaseDirectory + "NameClients.txt";
            string[] count = File.ReadAllLines(path); //список строк файла

            StreamReader reader = new StreamReader(path, Encoding.UTF8);

            name = count[r.Next(1, count.Length)]; //случайное имя
            reader.Close();
        } //присвоение случайного имени и фамилии клиенту
    } //класс клиента

    class WriteInfo
    {
        private int count; //счётчик для новых файлов
        private string nameFile; //имя файла 
        private static string pathToLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log"); // путь .Log

        public WriteInfo(int count, string nameFile)
        {
            this.count = count;
            this.nameFile = nameFile;
        }

        public void Write(string type, string mes)
        {
            string filename = Path.Combine(pathToLog, string.Format("Касса №" + count + ", кассир " + nameFile + ".log")); //название лог файла
            Writes(filename, type, mes);
        } //запись в лог файл

        public void WriteSeparator(char c)
        {
            string separator = new string(c, 100);
            string filename = Path.Combine(pathToLog, string.Format("Касса №" + count + ", кассир " + nameFile + ".log")); //название лог файла
            File.AppendAllText(filename, separator + "\n", Encoding.GetEncoding("Windows-1251")); //добавление строки в лог файл
        } //добавление разделителя

        private static void Writes(string fileName, string type, string mes)
        {
            object sync = new object(); //объект блокировки

            string fullText = string.Format("[{0:dd.MM.yyy HH:mm}] [{1}] [{2}]\r\n", DateTime.Now, type, mes);

            lock (sync)
            {
                File.AppendAllText(fileName, fullText, Encoding.GetEncoding("Windows-1251")); //добавление строки в лог файл
            }
        } //метод заполнения лог файла
    } //класс для записи в лог
}