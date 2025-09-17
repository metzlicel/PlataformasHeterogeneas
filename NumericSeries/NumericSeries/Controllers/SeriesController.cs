using Humanizer;
using Microsoft.AspNetCore.Mvc;
using NumericSeries.Models;

namespace NumericSeries.Controllers
{
    public class SeriesController : Controller
    {
        [HttpGet("/series/{series}/{n}")]
        public IActionResult Index(string series, int n = 0)
        {
            if (n < 0)
                return NotFound("El parámetro n debe ser mayor o igual a 0.");

            SeriesViewModel model = new SeriesViewModel();
            string csSeries = series.ToLower();
            
            switch (csSeries)
            {
                case "natural":
                    model = ReturnView("natural", n);
                    break;
                case "fibonacci":
                    model = ReturnView("fibonacci", n);
                    break;
                case "cuadraticos":
                    model = ReturnView("cuadraticos", n);
                    break;
                case "cubicos":
                    model = ReturnView("cubicos", n);
                    break;
                case "triangulares":
                    model = ReturnView("triangulares", n);
                    break;
                default:
                    return NotFound("La serie ingresada no existe.");
            }
            return View(model);
        }

        public SeriesViewModel ReturnView(string series, int n)
        {
            SeriesViewModel view = new SeriesViewModel();
            string anterior = null;
            string siguiente;

            if (n > 0)
            {
                anterior = $"/series/{series}/{n - 1}";
            }
            siguiente = $"/series/{series}/{n + 1}";

            switch (series)
            {
                case "natural":
                    view.Series = "Natural";
                    view.N = n;
                    view.Result = n;
                    view.NumAnterior = anterior;
                    view.NumSiguiente = siguiente;
                    break;
                case "fibonacci":
                    view.Series = "Fibonacci";
                    view.N = n;
                    view.Result = fibonacci(n);     
                    view.NumAnterior = anterior;
                    view.NumSiguiente = siguiente;
                    break;
                case "cuadraticos":
                    view.Series = "Cuadraticos";
                    view.N = n;
                    view.Result = n * n;
                    view.NumAnterior = anterior;
                    view.NumSiguiente = siguiente;
                    break;
                case "cubicos":
                    view.Series = "Cubicos";
                    view.N = n;
                    view.Result = n*n*n;   
                    view.NumAnterior = anterior;
                    view.NumSiguiente = siguiente;
                    break;
                case "triangulares":
                    view.Series = "Triangulares";
                    view.N = n;
                    view.Result = n*((n+1)/2);  
                    view.NumAnterior = anterior;
                    view.NumSiguiente = siguiente;
                    break;
            }
            return view;
        }

        private static long fibonacci(long n)
        {
            if (n <= 1) return n;
            long a = 0, b = 1;
            for (int i = 2; i <= n; i++)
            {
                long c = a + b;
                a = b;
                b = c;
            }
            return b;
        }
    }
}
