/PROG
  NAME: MainProgram
  COMMENT: "Полный демонстратор всех инструкций ASTRO"
  VERSION: 4.0
  AUTHOR: Demo
  RETURN_TYPE: INT
  MAX_CYCLES: 1000
/ATTR

!--- ARGUMENTS ---
  StartValue : INT = 0;
  ColorArg : COLOR = 0;

!--- LOCAL VARIABLES ---
  Counter : INT = 0;
  Sum : INT = 0;
  Message : STRING = '';
  IsEven : BOOL = FALSE;
  X : DOUBLE = 0;
  Temp : REAL = 0;

!--- CODE ---
/BODY
Counter := StartValue;  ! Counter = StartValue
Sum := 0;  ! Sum = 0
'START':
IF Counter >= 6 THEN GOTO 'END';  ! Выход если Counter >= 6
Sum := Sum + Counter;
IF (Counter % 2) == 0 THEN
  IsEven := TRUE;  ! Чётное
  ELSE
  IsEven := FALSE;  ! Нечётное
  END_IF;
FOR Counter := Counter + 1 TO Counter + 3 DO  ! FOR цикл
  Sum := Sum + Counter;
  IF Sum > 50 THEN
    BREAK;  ! Break если Sum>50
    END_IF;
  END_FOR;
Counter := Counter + 1;
IF (Counter % 2) == 0 THEN
  CONTINUE;  ! Пропуск чётных
  END_IF;
GOTO 'START';
'END':
RETURN Sum;
/END
