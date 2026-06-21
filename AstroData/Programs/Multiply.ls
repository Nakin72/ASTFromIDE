/PROG
  NAME: Multiply
  COMMENT: ""
  VERSION: 1.0
  AUTHOR: 
  RETURN_TYPE: INT
  MAX_CYCLES: 10
/ATTR

!--- ARGUMENTS ---
  A : INT = 0;
  B : INT = 0;

!--- LOCAL VARIABLES ---
  Result : INT = 0;

!--- CODE ---
/BODY
Result := A * B;
RETURN Result;
/END
