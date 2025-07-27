# Squaddy
This project fetches and displays game statistics for a specified Steam user using the MySquadStats and Steam APIs. The stats include the playtime of a [Squad](https://store.steampowered.com/app/393380/Squad/) and are displayed in a tabular format in the console.

## Requirements
- [.NET SDK](https://dotnet.microsoft.com/download)
- An environment variable `SQUADDY_STEAM_API_KEY` set with your Steam API key.

## Setup
1. **Clone the repository**:
```sh  
git clone <repository-url>
```
```sh
cd <repository-directory> 
```
2. **Set the Steam API Key**:  
Make sure to set the `SQUADDY_STEAM_API_KEY` environment variable with your Steam API key:
```sh 
export SQUADDY_STEAM_API_KEY=your_steam_api_key
```  
3. **Run the script**:  
Execute the script with the required arguments:
```sh 
dotnet fsi squaddy.fsx <username> <caseComparison> <matchType> 
```  
- `<username>`: The username to search for.
    - `<caseComparison>`: The case comparison type. Options are:
        -  `ignorecase` or `followcase`.
    - `<matchType>`: The match type. Options are:
        -  `contains` or `exactly`.

## Example
To run the script for a user with username "exampleUser", case-insensitive comparison, and exact match:
```sh
dotnet fsi squaddy.fsx "exampleUser" ignorecase exactly
```

## Thanks to
- [My Squad Stats](https://mysquadstats.com/)
