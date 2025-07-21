using Soneta.Business;
using Soneta.Kadry;
using Soneta.Tools;
using Soneta.Types;
using System;

namespace Rekrutacja.Workers
{
    public class ShapeAreaCalculatorWorker : BaseWorker
    {
        public class ShapeAreaCalculatorWorkerParameters : ContextBase
        {
            [Priority(1)]
            [Caption("A")]
            public int Arg1 { get; set; }

            [Priority(2)]
            [Caption("B")]
            public int Arg2 { get; set; }

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

        [Action("Kalkulator",
           Description = "Prosty kalkulator",
           Priority = 10,
           Mode = ActionMode.ReadOnlySession,
           Icon = ActionIcon.Accept,
           Target = ActionTarget.ToolbarWithText)]
        public string PerformAction()
        {
            double result = CalculateShapeArea();
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

        private int CalculateShapeArea()
        {
            switch (Parameters.Shape)
            {
                case ShapeEnum.Kwadrat:
                    return Parameters.Arg1 * Parameters.Arg1;
                case ShapeEnum.Prostokąt:
                    return Parameters.Arg1 * Parameters.Arg2;
                case ShapeEnum.Trojkąt:
                    return Parameters.Arg1 * Parameters.Arg2 / 2;
                case ShapeEnum.Koło:
                    return (int)(System.Math.PI * Parameters.Arg1 * Parameters.Arg1);
                default:
                    return 0;
            }
        }
    }
}

public enum ShapeEnum
{
    Kwadrat,
    Prostokąt,
    Trojkąt,
    Koło
}
