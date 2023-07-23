@echo "Please execute with administrator privileges 请用管理员权限执行"
@echo off
set "folderPath=%~dp0"
set "folderPath=%folderPath:~0,-1%"
echo "%folderPath%"
icacls "%folderPath%" /grant "Users:(OI)(CI)F" /T /C /Q

pause