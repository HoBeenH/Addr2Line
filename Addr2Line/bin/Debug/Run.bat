@echo on
set addrDir=%1
set soDir=%2
set memAddr=%3

%addrDir% -f -C -e %soDir%
%memAddr%
echo .