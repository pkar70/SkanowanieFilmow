# ChomikujApi
Reverse engineered (from web site) API in C# for managing http://www.chomikuj.pl account.

## Beware!

Api supports actions for user account only!

### Features:

* Get directories list
* Get file list in directory
* Create/Delete directory
* Download file
* Get direct download url to file
* Upload file
* Delete file

#### Extensibility :

Default REST and file handling is done by RestSharp but you easily can override it.

