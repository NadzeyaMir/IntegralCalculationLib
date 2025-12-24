using System;
using System.Threading;
using IntegralCalculationLib;

namespace IntegralConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Запуск одиночного потока ===");

            // 1. Создаем экземпляр калькулятора.
            var calculator = new IntegralCalculator();

            // 2. Подписываемся на событие прогресса.
            calculator.ProgressChanged += (sender, progress) =>
            {
                // Выводим прогресс в консоль.
                Console.WriteLine($"Прогресс: {progress:F2}%");
            };

            // 3. Подписываемся на событие завершения.
            calculator.CalculationCompleted += (sender, data) =>
            {
                // data - это кортеж (Result, Time)
                Console.WriteLine($"\nПоток {Thread.CurrentThread.ManagedThreadId}: Завершен с результатом: {data.Result:F6}");
                Console.WriteLine($"Время выполнения (тики): {data.Time}");
            };

            // 4. Запускаем вычисление в отдельном потоке.
            // Используем Task.Run, который помещает работу в пул потоков.
            Thread calculationThread = new Thread(new ThreadStart(calculator.CalculateIntegral));
            calculationThread.Start();

            // Ждем завершения потока, прежде чем перейти к следующей части.
            calculationThread.Join();

            // 5. Запуск двух потоков с разными приоритетами (Пункт 4).
            Console.WriteLine("\n=== Запуск двух потоков с разными приоритетами ===");

            (long timeHigh, double resultHigh) = (0, 0);
            (long timeLow, double resultLow) = (0, 0);

            var calcHigh = new IntegralCalculator();
            var calcLow = new IntegralCalculator();

            // Настраиваем обработчик завершения для первого потока (High priority).
            calcHigh.CalculationCompleted += (s, data) => { resultHigh = data.Result; timeHigh = data.Time; };
            // Настраиваем обработчик завершения для второго потока (Low priority).
            calcLow.CalculationCompleted += (s, data) => { resultLow = data.Result; timeLow = data.Time; };

            Thread threadHigh = new Thread(new ThreadStart(calcHigh.CalculateIntegral));
            Thread threadLow = new Thread(new ThreadStart(calcLow.CalculateIntegral));

            // Устанавливаем приоритеты.
            threadHigh.Priority = ThreadPriority.Highest;
            threadLow.Priority = ThreadPriority.Lowest;

            // Запускаем оба потока.
            threadHigh.Start();
            threadLow.Start();

            // Ожидаем завершения обоих.
            threadHigh.Join();
            threadLow.Join();

            // Выводим результаты.
            Console.WriteLine($"\nПоток с High приоритетом. Результат: {resultHigh:F6}, Время (тики): {timeHigh}");
            Console.WriteLine($"Поток с Low приоритетом.  Результат: {resultLow:F6}, Время (тики): {timeLow}");

            // 6. Тестируем синхронизацию (Пункт 5).
            TestSynchronization(1); // Должен работать только один поток

            // 7. Тестируем ограничение потоков семафором (Пункт 6).
            TestSynchronization(2); // Должны работать только два потока одновременно

            Console.ReadLine();
        }
        static void TestSynchronization(int maxParallelThreads)
        {
            Console.WriteLine($"\n=== Тест синхронизации (макс. потоков: {maxParallelThreads}) ===");
            IntegralCalculator.SetMaxParallelThreads(maxParallelThreads);

            int threadCount = 5;
            Thread[] threads = new Thread[threadCount];

            for (int i = 0; i < threadCount; i++)
            {
                int threadId = i + 1; // Для наглядности
                var calc = new IntegralCalculator();
                // Убираем подписку на прогресс, чтобы не засорять консоль.
                calc.CalculationCompleted += (s, data) =>
                {
                    Console.WriteLine($"Поток {threadId} завершил работу. Результат: {data.Result:F6}, Время: {data.Time}");
                };

                // Запускаем доработанный метод с синхронизацией.
                threads[i] = new Thread(() => calc.CalculateIntegralWithSync(threadId));
                threads[i].Start();

                // Небольшая задержка между запусками, чтобы их было легче различить.
                Thread.Sleep(50);
            }

            // Ожидаем завершения всех тестовых потоков.
            foreach (var t in threads)
            {
                t.Join();
            }
            Console.WriteLine("=== Тест завершен ===\n");
        }
    }
}