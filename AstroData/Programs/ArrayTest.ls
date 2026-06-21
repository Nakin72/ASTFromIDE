/PROG
  NAME: ArrayTest
  COMMENT: "Тест массивов и встроенных функций"
  VERSION: 1.0
  AUTHOR: Demo
  RETURN_TYPE: INT
  MAX_CYCLES: 100
/ATTR

!--- LOCAL VARIABLES ---
  Sum : INT = 3;
  Item : INT = 5;
  Size : INT = -1;
  MyArray : STRING = [1, 2, 3, 4, 5];
  Message : STRING = 'Hello World';
  X : DOUBLE = 4;

!--- CODE ---
/BODY
Size := SIZE(MyArray);  ! Size = SIZE(MyArray)
FOR_EACH Item IN MyArray DO  ! FOREACH Item IN MyArray
  Sum := Sum + Item;  ! Sum = Sum + Item
  END_FOR_EACH;
Item := 10;  ! Item = 10
Size := ADD(MyArray, Item);  ! ADD(MyArray, 10)
Size := FIND(MyArray, 3);  ! Index = FIND(MyArray, 3)
Sum := SIZE(SLICE(MyArray, 1, 3));  ! Size = SIZE(SLICE(MyArray, 1, 3))
MyArray := RANGE(1, 5, 1);  ! MyArray = RANGE(1, 5, 1)
X := SIN(3.14159 / 2);  ! X = SIN(PI/2)
X := SQRT(16);  ! X = SQRT(16)
Message := 'Hello World';  ! Message = 'Hello World'
RETURN Sum;
/END
