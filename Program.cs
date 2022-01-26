using System;
using System.Threading;

namespace AsyncProgram_1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Введите название магазина: ");
            string Tittle = Console.ReadLine();
            Console.Write("Введите количество касс: ");
            sbyte cashCount = sbyte.Parse(Console.ReadLine());
            Console.Write("Введите количество посетителей: ");
            int people = Convert.ToInt32(Console.ReadLine());

            Shop shop = new Shop(Tittle, people, cashCount);
            
            shop.Start();

            Console.ReadKey();

            Console.WriteLine("Общий баланс магазина " + shop.Balance);
        }
    }
}
