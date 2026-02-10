using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TestTask
{
    public class Program
    {
        // Список гласных
        private static readonly HashSet<char> Vowels = new HashSet<char>(
            "аеёиоуыэюяAEIOUY".ToUpperInvariant()
        );

        /// <summary>
        /// Программа принимает на входе 2 пути до файлов.
        /// Анализирует в первом файле кол-во вхождений каждой буквы (регистрозависимо). Например А, б, Б, Г и т.д.
        /// Анализирует во втором файле кол-во вхождений парных букв (не регистрозависимо). Например АА, Оо, еЕ, тт и т.д.
        /// По окончанию работы - выводит данную статистику на экран.
        /// </summary>
        /// <param name="args">Первый параметр - путь до первого файла.
        /// Второй параметр - путь до второго файла.</param>
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Укажите два пути до файлов.");
                return;
            }

                using (IReadOnlyStream inputStream1 = GetInputStream(args[0]))
                using (IReadOnlyStream inputStream2 = GetInputStream(args[1]))
                {
                    IList<LetterStats> singleLetterStats = FillSingleLetterStats(inputStream1);
                    IList<LetterStats> doubleLetterStats = FillDoubleLetterStats(inputStream2);

                    // Оставляем все буквы и выводим первоначальную статистику в консоль
                    Console.WriteLine("Полная статистика");
                    PrintStatistic(singleLetterStats);
                    PrintStatistic(doubleLetterStats);

                    // Убираем гласные и согласные из статистики и выводим результат в консоль
                    RemoveCharStatsByType(singleLetterStats, CharType.Vowel);
                    RemoveCharStatsByType(doubleLetterStats, CharType.Consonants);

                    Console.WriteLine("Статистика без учёта гласных в первом файле и согласных во втором файле");
                    PrintStatistic(singleLetterStats);
                    PrintStatistic(doubleLetterStats);
                }

            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey(true);
        }


        /// <summary>
        /// Ф-ция возвращает экземпляр потока с уже загруженным файлом для последующего посимвольного чтения.
        /// </summary>
        /// <param name="fileFullPath">Полный путь до файла для чтения</param>
        /// <returns>Поток для последующего чтения.</returns>
        private static IReadOnlyStream GetInputStream(string fileFullPath)
        {
            return new ReadOnlyStream(fileFullPath);
        }

        /// <summary>
        /// Ф-ция считывающая из входящего потока все буквы, и возвращающая коллекцию статистик вхождения каждой буквы.
        /// Статистика РЕГИСТРОЗАВИСИМАЯ!
        /// </summary>
        /// <param name="stream">Стрим для считывания символов для последующего анализа</param>
        /// <returns>Коллекция статистик по каждой букве, что была прочитана из стрима.</returns>
        private static IList<LetterStats> FillSingleLetterStats(IReadOnlyStream stream)
        {
            var dict = new Dictionary<string, LetterStats>();
            stream.ResetPositionToStart();

            while (!stream.IsEof)
            {
                char c;
                try { c = stream.ReadNextChar(); }
                catch (EndOfStreamException) { break; }

                if (!char.IsLetter(c)) continue;

                string key = c.ToString();
                if (!dict.ContainsKey(key))
                    dict[key] = new LetterStats { Letter = key };
                IncStatistic(dict[key]);
            }

            return dict.Values.ToList();
        }


        /// <summary>
        /// Ф-ция считывающая из входящего потока все буквы, и возвращающая коллекцию статистик вхождения парных букв.
        /// В статистику должны попадать только пары из одинаковых букв, например АА, СС, УУ, ЕЕ и т.д.
        /// Статистика - НЕ регистрозависимая!
        /// </summary>
        /// <param name="stream">Стрим для считывания символов для последующего анализа</param>
        /// <returns>Коллекция статистик по каждой букве, что была прочитана из стрима.</returns>
        private static IList<LetterStats> FillDoubleLetterStats(IReadOnlyStream stream)
        {
            var dict = new Dictionary<string, LetterStats>();
            char? prev = null;

            stream.ResetPositionToStart();

            while (!stream.IsEof)
            {
                char current;
                try { current = stream.ReadNextChar(); }
                catch (EndOfStreamException) { break; }

                if (!char.IsLetter(current))
                {
                    prev = null;
                    continue;
                }

                char normalized = char.ToUpperInvariant(current);

                if (prev.HasValue && char.ToUpperInvariant(prev.Value) == normalized)
                {
                    string pair = $"{normalized}{normalized}";
                    if (!dict.ContainsKey(pair))
                        dict[pair] = new LetterStats { Letter = pair };
                    IncStatistic(dict[pair]);
                }

                prev = current;
            }

            return dict.Values.ToList();
        }


        /// <summary>
        /// Ф-ция перебирает все найденные буквы/парные буквы, содержащие в себе только гласные или согласные буквы.
        /// (Тип букв для перебора определяется параметром charType)
        /// Все найденные буквы/пары соответствующие параметру поиска - удаляются из переданной коллекции статистик.
        /// </summary>
        /// <param name="letters">Коллекция со статистиками вхождения букв/пар</param>
        /// <param name="charType">Тип букв для анализа</param>

        private static void RemoveCharStatsByType(IList<LetterStats> letters, CharType charType)
        {
            for (int i = letters.Count - 1; i >= 0; i--)
            {
                string val = letters[i].Letter;
                if (!val.All(char.IsLetter)) continue;

                bool isVowelOnly = val.All(c => Vowels.Contains(char.ToUpperInvariant(c)));
                bool isConsonantOnly = val.All(c => char.IsLetter(c) && !Vowels.Contains(char.ToUpperInvariant(c)));

                if ((charType == CharType.Vowel && isVowelOnly) ||
                    (charType == CharType.Consonants && isConsonantOnly))
                {
                    letters.RemoveAt(i);
                }
            }
        }


        /// <summary>
        /// Ф-ция выводит на экран полученную статистику в формате "{Буква} : {Кол-во}"
        /// Каждая буква - с новой строки.
        /// Выводить на экран необходимо предварительно отсортировав набор по алфавиту.
        /// В конце отдельная строчка с ИТОГО, содержащая в себе общее кол-во найденных букв/пар
        /// </summary>
        /// <param name="letters">Коллекция со статистикой</param>
        private static void PrintStatistic(IEnumerable<LetterStats> letters)
        {
            int total = 0;

            foreach (var stat in letters.OrderBy(l => l.Letter, StringComparer.Ordinal))
            {
                Console.WriteLine($"{stat.Letter} : {stat.Count}");
                total += stat.Count;
            }

            Console.WriteLine($"ИТОГО : {total}");
        }


        /// <summary>
        /// Метод увеличивает счётчик вхождений по переданной структуре.
        /// </summary>
        /// <param name="letterStats"></param>
        private static void IncStatistic(LetterStats letterStats)
        {
            letterStats.Count++;
        }


    }
}
