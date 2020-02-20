using System;

namespace Bowerbird
{
    static class Integrate
    {
        public static double AdaptiveSimpson(Func<double, double> f, double a, double b, double tolerance)
        {
            var tol_factor = 10.0;

            var h = 0.5 * (b - a);

            var x0 = a;
            var x1 = a + 0.5 * h;
            var x2 = a + h;
            var x3 = a + 1.5 * h;
            var x4 = b;

            var f0 = f(x0);
            var f1 = f(x1);
            var f2 = f(x2);
            var f3 = f(x3);
            var f4 = f(x4);

            var s0 = h * (f0 + 4.0 * f2 + f4) / 3.0;
            var s1 = h * (f0 + 4.0 * f1 + 2.0 * f2 + 4.0 * f3 + f4) / 6.0;

            var s = default(double);

            if (Math.Abs(s0 - s1) >= tol_factor * tolerance)
                s = AdaptiveSimpson(f, x0, x2, 0.5 * tolerance) + AdaptiveSimpson(f, x2, x4, 0.5 * tolerance);
            else
                s = s1 + (s1 - s0) / 15.0;

            return s;
        }
    }
}
