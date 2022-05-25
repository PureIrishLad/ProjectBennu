using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utility
{
    // Modulo function that produces correct results for n < 0
    public static int Modulo(int n, int m)
    {
        int r = n % m;
        return r < 0 ? r + m : r;
    }
    public static float ModuloF(float n, int m)
    {
        float r = n % m;
        return r < 0 ? r + m : r;
    }

    public static byte ModuloB(int n, byte m)
    {
        return (byte)(n % m);
    }
}
