call "cmd /c start .\bin\server"
timeout 5
exho "Runing clients"
call "cmd /c start .\bin\client details"
call "cmd /c start .\bin\client master"
call "cmd /c start .\bin\client both"
