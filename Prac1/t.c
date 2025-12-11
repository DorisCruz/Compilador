#include <stdio.h>

int main() {

    int x = 10;
    float y = 5.5;

    if (x > 0) {
        printf("Positivo");
    }

    for (int i = 0; i < 10; i++) {
        x++;
}
    sitch (x) {
        case 1;
            printf("Uno");
        break;
        default:
            printf("Otro");
        break;
    }

    do {
        x++;
    } while (x < 20)

    while (x < 30) {
        x++;
    }

    return 0;
}