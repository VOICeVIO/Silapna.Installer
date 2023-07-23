cd Silapna.Desktop
set ProfilePath=Properties\\PublishProfiles\\FolderProfile.pubxml
set PublishPath=publish

dotnet publish --configuration Release /p:PublishProfile="%ProfilePath%" /p:PublishDir="%PublishPath%"