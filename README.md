# Abstract Language Model

## Feed

### Обновление 0.1.12 (10.02.2024)
- Временно удалены нестабильные операторы `+:`, `-:`, `*:`, `/:`.
- Интерпретатор разделен на части. Стало легче управлять им. Упрощен код с помощью паттернов.
- Изменена конструкция Parser-а. Осуществляется слежка указателя при чтении кода. Ошибки Parser-а теперь указывают место ошибки.
```alm
print(1 + );
Expected expression at line 1 column 9
```
- Ключевое слово `print` теперь нуждается в скобках.
```alm
print(E * PI);
```

### Update 0.1.10 (04.12.2023)
- Added operators `+:`, `-:`, `*:`, `/:`.
```alm
data A;
A +: 3;
A : A + 3;
```
- Optimized token parsing.

### Update 0.1.9 (29.11.2023)
- Added the keyword `null`. To use a missing value, it's necessary to explicitly use `null`. The absence of a value is no longer automatically considered as `null`.
```alm
data A : null;
```
- Improved recognition of semicolons.
- Now the initial variables `E` and `Pi` are considered non-writable. Their values cannot be changed.
- Improved adaptability and interpretation of variables.
```alm
print(data A : 5);
```
- Fixed a bug where it was possible to initialize a variable with itself.


### Update 0.1.7 (28.11.2023)
- Now you can execute code with multiple instructions.
```alm
data A : 1;
data B : 2;
print(A);
print(B);
```
- Added the ability to work with signed numbers. For example: `+2`, `-4`.
```alm
print(7 + -4);
```
- Improved language adaptability. Now, the absence of a value anywhere will be interpreted as `null`;
```alm
data A : ();
print(A);
```
- Changed syntax. Now it's mandatory to put a semicolon after each instruction.

### Update 0.1.5 (27.11.2023)
- Optimized parsing of code in brackets.
- Improved error descriptions.
- Added the ability to put a semicolon at the end of a line. It can also be omitted.
- Added the ability to declare variables, initialize them, and change their values.
- Now, only data in the `print()` key block will be output to the console.

### Release 0.1.2 (24.11.2023)
First stable version