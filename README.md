# Adaptive Network Language

## Feed

### Release 0.1.2 (24.11.2023)
First stable version

### Update 0.1.5 (27.11.2023)
- Optimized parsing of code in brackets.
- Improved error descriptions.
- Added the ability to put a semicolon at the end of a line. It can also be omitted.
- Added the ability to declare variables, initialize them, and change their values.
- Now, only data in the `print()` key block will be output to the console.

### Update 0.1.7 (28.11.2023)
- Now you can execute code with multiple instructions.
```anl
data A: 1;
data B: 2;
print(A);
print(B);
```
- Added the ability to work with signed numbers. For example: `+2`, `-4`.
```anl
print(7 + -4);
```
- Improved language adaptability. Now, the absence of a value anywhere will be interpreted as `null`;
```anl
data A: ;
print(A);
```
- Changed syntax. Now it's mandatory to put a semicolon after each instruction.