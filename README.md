# Conway's Game of Life + AI Visualization

This project implements Conway's Game of Life cellular automaton with an AI-powered visualization feature. The application allows users to run Game of Life simulations and generate AI images based on the patterns that emerge.

## Features

- Interactive Game of Life simulation with a customizable grid
- Pre-configured patterns like Glider, Blinker, Pulsar, and Gosper Glider Gun
- Colorful cell visualization
- AI image generation based on the current pattern

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download) (or latest version)
- A modern web browser
- [Visual Studio Code](https://code.visualstudio.com/) with the following extensions:
  - [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)
  - [.NET Core Tools](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.vscode-dotnet-runtime)

## Getting Started

### Clone the Repository

```bash
git clone https://github.com/yourusername/conways-gol-ai-hack.git
cd conways-gol-ai-hack
```

### API Setup for AI Image Generation

The application uses an external API for AI image generation. You'll need to set up an API key for the EverArt service:

1. Copy `ImageGenerationProxyApi/appsettings.Example.json` to `ImageGenerationProxyApi/appsettings.json`
2. Update the `EverArt:ApiKey` value with your API key
3. If you don't have an API key, the application will still run, but image generation functionality will not work

### Building and Running the Application

#### Using Visual Studio Code

To run both the Blazor app and API service:

1. Open the repository folder in Visual Studio Code:
   ```bash
   code .
   ```

2. First, start the ImageGenerationProxyApi:
   ```bash
   cd ImageGenerationProxyApi
   dotnet run
   ```
   This will start the API service at:
   - HTTPS: https://localhost:5062
   - HTTP: http://localhost:5063

3. Open a new terminal (Terminal → New Terminal) and start the Blazor app:
   ```bash
   cd ConwaysGameOfLife
   dotnet run
   ```
   This will start the Blazor application at:
   - HTTPS: https://localhost:7253
   - HTTP: http://localhost:5099

4. Open your browser and navigate to the Blazor app URL (https://localhost:7253)

#### Using .NET CLI

You can also run both applications manually in separate terminal windows:

```bash
# Terminal 1 - API Service
cd ImageGenerationProxyApi
dotnet restore
dotnet build
dotnet run

# Terminal 2 - Blazor App
cd ConwaysGameOfLife
dotnet restore
dotnet build
dotnet run
```

## How to Use

1. **Start/Pause Simulation**: Click the Start/Pause button or press the Spacebar
2. **Reset Grid**: Click the Reset button to clear the grid
3. **Select Patterns**: Choose a pattern from the dropdown menu and click "Apply Pattern"
4. **Draw Your Own**: Click on cells to toggle them on/off when the simulation is paused
5. **Change Speed**: Adjust the speed slider to control the simulation speed
6. **Generate AI Image**: 
   - Run the simulation until an interesting pattern appears
   - Pause the simulation
   - Click "Generate AI Image" to create a visualization based on the current pattern

## Understanding Conway's Game of Life

Conway's Game of Life is a cellular automaton devised by mathematician John Conway. It follows these simple rules:

1. Any live cell with fewer than two live neighbors dies (underpopulation)
2. Any live cell with two or three live neighbors lives on (survival)
3. Any live cell with more than three live neighbors dies (overpopulation)
4. Any dead cell with exactly three live neighbors becomes alive (reproduction)

These simple rules lead to fascinating emergent patterns and behaviors.

## About the AI Visualization

When you generate an AI image, the application:
1. Analyzes the current pattern to identify features like symmetry, density, and clustering
2. Creates a detailed text prompt based on these characteristics
3. Sends the prompt to an image generation API
4. Displays the resulting image alongside information about the prompt

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
