# map2stl


## Description
Map2stl is a web application for viewing and generating detailed 3d models ready for print from heightmaps.

## Features
Map-based Selection: Pick any area of the map interactively.
Elevation Data Integration: Retrieve terrain details and altitude data.
Customizable Output: Export to standard 3D formats (OBJ, STL, GLTF, etc.).
REST API: Interact with the model generation logic via a .NET Web API.
CLI Tool: Use the NPM package for quick generation via the command line.
Cross-platform: Works on Windows, macOS, and Linux.

## Tech Stack
Backend/API: .NET 6 (or higher)
Frontend: web-based npm front-end
Package Management: npm
Mapping Services: OpenStreetMap, https://www.geoportal.sk/sk/zbgis/ortofotomozaika/
3D Libraries: Three.js 
Build Tools:
.NET CLI for building the backend
npm for managing React dependencies

## Installation
1. Clone the repository:
https://github.com/PeterPukalik/map2stl.git
2. Install dependencies:

cd map2stl/map2stl/
	dotnet restore
	dotnet build

cd ../../fe
	npm install
3. Run the application:
cd map2stl/map2stl/
	dotnet run

cd ../../fe
	npm start