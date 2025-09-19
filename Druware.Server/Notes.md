## Publishing To Internal

In order to maintain this locally and securely, we are maintaining our packages
in an internal shared folder. In order to publish this package the .nuspec
version needs to be incremented:

```
    <version>0.1.0</version>
```

Next the project needs to built in its Release form:

```
    dotnet build . --configuration RELEASE 
```

With that built, the package itself can be produced and pushed to the shared
folder.  The following example uses that Mac style path, but it could be
replaced with a Windows share path using the
\\192.168.10.25\share\Developers\Packages\ style path.

```
nuget pack -OutputDirectory pub -Properties Configuration=Release
cd pub 
nuget add Druware.Server.version.nupkg -Source /Volumes/share/Developer/Packages
```