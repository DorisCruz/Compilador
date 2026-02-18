#include <stdio.h>

float Suma(float a, float b);

int main()
{
    float Resultado, a, b;

    a = 3.6;
    b = 2.5;

    Resultado = Suma(a, b);

    return 0;
}

float Suma( float a, float b)
{
    return a + b;
}