using Rekrutacja.Workers;
using Soneta.Business;
using Soneta.Kadry;

[assembly: Worker(typeof(ShapeAreaCalculatorWorker), typeof(Pracownicy))]
namespace Rekrutacja.Workers
{
    public class BaseWorker
    {
    }
}
