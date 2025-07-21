using System;
using System.Collections.Generic;
using System.Linq;
using Soneta.Business;
using Soneta.Kadry;
using Soneta.Tools;
using Soneta.Types;

namespace Rekrutacja.Workers
{
    public class CalculatorWithCustomParserWorker
    {
        public class ShapeAreaCalculatorWorkerParameters : ContextBase
        {
            [Priority(1)]
            [Caption("A")]
            public string Arg1 { get; set; }

            [Priority(2)]
            [Caption("B")]
            public string Arg2 { get; set; }

            [Priority(3)]
            [Caption("Operacja")]
            public ShapeEnum Shape { get; set; }

            [Priority(4)]
            [Caption("Data obliczeń")]
            public Date CalculationDate { get; set; }

            public ShapeAreaCalculatorWorkerParameters(Context context) : base(context)
            {
                CalculationDate = Date.Today;
            }
        }

        [Context]
        public Context Cx { get; set; }

        [Context]
        public ShapeAreaCalculatorWorkerParameters Parameters { get; set; }

        private Dictionary<ShapeEnum, Func<string, string, bool>> _shapeValueValidationMap = 
            new Dictionary<ShapeEnum, Func<string, string, bool>>
        {
            {ShapeEnum.Kwadrat, (string value1, string value2) => IsCorrectValue(value1) },
            {ShapeEnum.Prostokąt, (string value1, string value2) => IsCorrectValue(value1) && IsCorrectValue(value2) },
            {ShapeEnum.Trojkąt, (string value1, string value2) => IsCorrectValue(value1) && IsCorrectValue(value2) },
            {ShapeEnum.Koło, (string value1, string value2) => IsCorrectValue(value1) }
        };

        private Dictionary<char, int> _charNumberMap = new Dictionary<char, int>
        {
            { '1', 1 }, { '2', 2 }, { '3', 3 }, { '4', 4 }, { '5', 5 }, { '6', 6 }, { '7', 7 }, { '8', 8 }, { '9', 9 }, 
            { '0', 0 },
        };

        [Action("Kalkulator",
           Description = "Prosty kalkulator",
           Priority = 10,
           Mode = ActionMode.ReadOnlySession,
           Icon = ActionIcon.Accept,
           Target = ActionTarget.ToolbarWithText)]
        public string PerformAction()
        {
            if (_shapeValueValidationMap.TryGetValue(Parameters.Shape, out Func<string, string, bool> validate) &&
               !validate(Parameters.Arg1, Parameters.Arg2))
                return "Wprowadzona wartość nie jest liczbą. Wartość w kolumnie Wynik pozostała niezmieniona.";

            double result = CalculateShapeArea(Parse(Parameters.Arg1), Parse(Parameters.Arg2));
            if (result < 0)
                return "Wynik nie może być liczbą ujemną. Wartość w kolumnie Wynik pozostała niezmieniona.";

            DebuggerSession.MarkLineAsBreakPoint();

            Pracownik[] workers = { };
            if (Cx.Contains(typeof(Pracownik[])))
                workers = (Pracownik[])Cx[typeof(Pracownik[])];

            using (Session newSession = Cx.Login.CreateSession(false, false, "ModyfikacjaPracownika"))
            {
                using (ITransaction trans = newSession.Logout(true))
                {
                    foreach (var worker in workers)
                    {
                        var workerFromSession = newSession.Get(worker);

                        workerFromSession.Features["DataObliczen"] = Parameters.CalculationDate;
                        workerFromSession.Features["Wynik"] = result;
                    }
                    trans.CommitUI();
                }
                newSession.Save();
            }

            return null;
        }

        private static bool IsCorrectValue(string value)
        {
            return !string.IsNullOrEmpty(value) && value.All(x => char.IsDigit(x));
        }

        private int Parse(string value)
        {
            if(!IsCorrectValue(value))
                return 0;

            if (value.Length == 1 && _charNumberMap.TryGetValue(value[0], out int parsedValue))
                return parsedValue;

            int sum = 0;
            int multipleFactor = 10;
            for (int i = 0; i < value.Length; i++)
                sum += _charNumberMap[value[i]] * (int)(System.Math.Pow(multipleFactor, value.Length - i - 1));

            return sum;
        }

        private int CalculateShapeArea(int arg1, int arg2)
        {
            switch (Parameters.Shape)
            {
                case ShapeEnum.Kwadrat:
                    return arg1 * arg1;
                case ShapeEnum.Prostokąt:
                    return arg1 * arg2;
                case ShapeEnum.Trojkąt:
                    return arg1 * arg2 / 2;
                case ShapeEnum.Koło:
                    return (int)(System.Math.PI * arg1 * arg1);
                default:
                    return 0;
            }
        }
    }
}
