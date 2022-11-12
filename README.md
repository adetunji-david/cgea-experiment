# The Competing Genes Evolutionary Algorithm (cgEA)

This project generates runtime data for the partial-restart cgEA and the smart-restart cGA on three benchmark problems.
Note that this project requires [.NET 6.0](https://dotnet.microsoft.com/en-us/download) SDK, and the ```generate_plots.py``` file requires an installation of python3 with the pandas and seaborn packages.

## Build
Download the source code and navigate to the root of the project in a terminal. Then run

```bash
dotnet build --configuration release
cd ./bin/Release
```
to build the code.
## Execute
To generate the RunningTime files, run
### Unix-like
```bash
./bin/Release/net6.0/CgeaExperiment
``` 
### Windows

```bash
.\bin\Release\net6.0\CgeaExperiment.exe
``` 

## License
[MIT](https://choosealicense.com/licenses/mit/)