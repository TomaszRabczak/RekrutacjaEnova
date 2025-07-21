using Rekrutacja.Workers;
using Soneta.Business;
using Soneta.Kadry;
using Soneta.Types;
using Soneta.Tools;
using System.Linq;

[assembly: Worker(typeof(CalculatorWorker), typeof(Pracownicy))]
namespace Rekrutacja.Workers
{
    public class CalculatorWorker
    {
        public class CalculatorWorkerParameters : ContextBase
        {
            [Priority(1)]
            [Caption("A")]
            public double Arg1 {  get; set; }

            [Priority(2)]
            [Caption("B")]
            public double Arg2 { get; set; }

            [Priority(3)]
            [Caption("Operacja")]
            public string Operation {  get; set; }

            [Priority(4)]
            [Caption("Data obliczeń")]
            public Date CalculationDate { get; set; }

            public CalculatorWorkerParameters(Context context) : base(context)
            {
                CalculationDate = Date.Today;
            }
        }

        [Context]
        public Context Cx { get; set; }

        [Context]
        public CalculatorWorkerParameters Parameters { get; set; }

        private string[] _allowedOperations = { "+", "-", "*", "/" };

        [Action("Kalkulator",
           Description = "Prosty kalkulator",
           Priority = 10,
           Mode = ActionMode.ReadOnlySession,
           Icon = ActionIcon.Accept,
           Target = ActionTarget.ToolbarWithText)]
        public string PerformAction()
        {
            string validationError = GetValidationError();
            if(validationError != null)
                return validationError;

            double result = PerformSimpleCalculation(Parameters.Arg1, Parameters.Arg2,
                            Parameters.Operation);
            if (result < 0)
                return "Wynik nie może być liczbą ujemną. Wartość w kolumnie Wynik pozostała niezmieniona.";

            DebuggerSession.MarkLineAsBreakPoint();

            Pracownik[] workers = {};
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

        private string GetValidationError()
        {
            if (Parameters.Operation == "/" && Parameters.Arg2 == 0)
                return "Dzielenie przez 0 jest niedozwolone. Wartość w kolumnie Wynik pozostała niezmieniona.";
            else if (!_allowedOperations.Contains(Parameters.Operation))
                return "Niepoprawna wartość w polu Operacja. Wartość w kolumnie Wynik pozostała niezmieniona.";

            return null;
        }

        private double PerformSimpleCalculation(double arg1, double arg2, string operation)
        {
            switch (operation)
            {
                case "+":
                    return arg1 + arg2;
                case "-":
                    return arg1 - arg2;
                case "*":
                    return arg1 * arg2;
                case "/":
                    return arg1 / arg2;
                default:
                    return 0;
            }
        }
    }
}
