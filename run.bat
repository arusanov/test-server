exho "Generating sets"
.\bin\DataSetGenerator .\DataSets\ 100000 100
call "cmd /c start .\bin\server"
timeout 5
exho "Runing clients"
call "cmd /c start .\bin\client details"
call "cmd /c start .\bin\client master"
call "cmd /c start .\bin\client both"
