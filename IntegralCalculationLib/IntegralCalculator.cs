using System;
using System.Diagnostics;
using System.Threading;

namespace IntegralCalculationLib
{
    public class IntegralCalculator
    {
        // Событие для передачи прогресса. Будет отправлять текущий процент выполнения.
        public event EventHandler<double> ProgressChanged;
        // Событие для передачи результата по завершению. Будет отправлять результат и время.
        public event EventHandler<(double Result, long Time)> CalculationCompleted;

        // Объект для синхронизации (для пункта 5, где может работать только один поток).
        private static readonly object _lockObject = new object();
        // Семафор для ограничения количества одновременно работающих потоков (для пункта 6).
        // Например, SemaphoreSlim(2, 2) позволит работать только двум потокам.
        private static SemaphoreSlim _threadLimiter = new SemaphoreSlim(1, 1); // Начнем с одного

        // Метод для настройки семафора извне (для пункта 6).
        public static void SetMaxParallelThreads(int maxCount)
        {
            _threadLimiter = new SemaphoreSlim(maxCount, maxCount);
        }
        // Основной метод вычисления интеграла.
        public void CalculateIntegral()
        {
            // Начинаем замер времени.
            Stopwatch stopwatch = Stopwatch.StartNew();

            double a = 0.0; // Начало отрезка
            double b = 1.0; // Конец отрезка
            double step = 0.00000001; // Шаг итерации
            long iterations = (long)((b - a) / step); // Общее количество итераций
            double sum = 0.0; // Сумма площадей прямоугольников
            long currentIteration = 0; // Текущая итерация

            // Основной цикл метода прямоугольников.
            for (double x = a; x < b; x += step)
            {
                // Вычисляем высоту прямоугольника (значение функции sin(x) в средней точке).
                double y = Math.Sin(x + step / 2);
                // Добавляем площадь текущего прямоугольника к общей сумме.
                sum += y * step;

                currentIteration++;

                // Искусственная задержка для увеличения времени работы (как в задании, но от этого программа очень долго работает
                // чтобы проверить лучше уменьшить до 100 или 1000).
                for (int j = 0; j < 100000; j++)
                {
                    var dummy = 123 * 456; // Формальное вычисление
                }

                // Примерно каждые 1% итераций сообщаем о прогрессе.
                if (currentIteration % (iterations / 100) == 0)
                {
                    double progressPercentage = (double)currentIteration / iterations * 100;
                    // Вызываем событие ProgressChanged, передавая процент.
                    OnProgressChanged(progressPercentage);
                }
            }

            // Останавливаем таймер.
            stopwatch.Stop();
            double result = sum;

            // Вызываем событие CalculationCompleted, передавая результат и время в тиках.
            OnCalculationCompleted(result, stopwatch.ElapsedTicks);
        }
        // Метод для вызова события прогресса.
        protected virtual void OnProgressChanged(double progress)
        {
            // Проверка на наличие подписчиков. Если есть, вызываем событие.
            ProgressChanged?.Invoke(this, progress);
        }

        // Метод для вызова события завершения расчета.
        protected virtual void OnCalculationCompleted(double result, long time)
        {
            CalculationCompleted?.Invoke(this, (result, time));
        }
        // Метод для вычисления интеграла с поддержкой ограничения потоков.
        public void CalculateIntegralWithSync(int threadId)
        {
            // Пункт 6: Поток пытается войти в семафор. Если мест нет - ждет.
            _threadLimiter.Wait();
            try
            {
                lock (_lockObject)
                {
                    Console.WriteLine($"Поток {threadId}: начал вычисление.");
                    CalculateIntegral(); // Вызываем наш основной метод расчета
                    Console.WriteLine($"Поток {threadId}: завершил вычисление.");
                }
            }
            finally
            {
                _threadLimiter.Release();
            }
        }
    }
}
