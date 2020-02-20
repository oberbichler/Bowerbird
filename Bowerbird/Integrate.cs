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

        public static double Romberg(Func<double, double> f, double a, double b, double tolerance, int maxIter)
        {
            var r1 = new double[maxIter];
            var r2 = new double[maxIter];

            var Rp = r1; // current row
            var Rc = r2; // previous row

            double h = (b - a); // step size

            Rp[0] = (f(a) + f(b)) * h * 0.5; // first trapezoidal step

            for (int i = 1; i < maxIter; ++i)
            {
                h /= 2.0;

                double c = 0;

                int ep = 1 << (i - 1);

                for (int j = 1; j <= ep; ++j)
                    c += f(a + (2 * j - 1) * h);

                Rc[0] = h * c + 0.5 * Rp[0]; // R(i,0)

                for (int j = 1; j <= i; ++j)
                {
                    var n_k = Math.Pow(4.0, j);
                    Rc[j] = (n_k * Rc[j - 1] - Rp[j - 1]) / (n_k - 1); // compute R(i,j)
                }

                if (i > 1 && Math.Abs(Rp[i - 1] - Rc[i]) < tolerance)
                    return Rc[i - 1];

                // swap Rn and Rc as we only need the last row
                var rt = Rp;

                Rp = Rc;
                Rc = rt;
            }

            return Rp[maxIter - 1];
        }
    }
}
