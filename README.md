# LocalAIAssistant
## Ollama Commands
- **ollama serve**: Starts the Ollama service, which is necessary for running models. This command is typically run in the background or as a service.
- **ollama run <model_name>**: Runs a specified model in an interactive chat session. If the model is not already downloaded, Ollama will automatically pull it.
- **ollama pull <model_name>**: Downloads a specified model from the Ollama registry without immediately running it.
- **ollama list**: Displays a list of all models currently downloaded on your local system.
- **ollama ps**: Shows a list of models that are currently running or loaded in memory.
- **ollama stop <model_name>**: Stops a specific running model.
- **ollama rm <model_name>**: Removes a downloaded model from your system, freeing up disk space.
- **ollama cp <source_model> <destination_model>**: Copies an existing model to create a new reference, without duplicating the full model file.
- **ollama create <model_name> -f <path_to_modelfile>**: Creates a new model based on a Modelfile, which defines the model's configuration and parameters.
- **ollama show <model_name>**: Displays detailed information about a specific model, including its architecture, parameters, and license.
- **ollama push <model_name>**: Uploads a local model to the Ollama registry (requires an Ollama account and API keys).
- **ollama --help**: Provides a comprehensive list and description of all available Ollama commands and their options.