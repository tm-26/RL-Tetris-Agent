The TetrisTrain folder contains all of the resources used to train multiple agents to play a game of Tetris.
 -In the results sub-folder one can find all of the 3 trained models.
 -To resume training:
 --Setup a Unity and Python environment as explained in:https://github.com/Unity-Technologies/ml-agents/blob/master/docs/Getting-Started.md
 --Tensorflow need to also be installed on your machine.
 --cd into the TetrisTrain directory.
 --In the results folder, rename the model you want to tain to TetrisSinglePlayer.
 --run the command: mlagents-learn Assets/results/TetrisSinglePlayer/configuration.yaml --run-id=TetrisSinglePlayer --resume
 --Press the play button in the Unity editor.

The ProjectTetrisTrain folder contains all of the resources used to train multiple agents to play a game of Project Tetris
 -In the results sub-folder one can find the 2 trained models.
 -To resume training:
 --Setup a Unity and Python environment as explained in:https://github.com/Unity-Technologies/ml-agents/blob/master/docs/Getting-Started.md
 --Tensorflow need to also be installed on your machine.
 --cd into the ProjectTetrisTrain directory.
 --To resume training for the agent that takes in 1 CNN as input run the command: mlagents-learn Assets/results/TetrisMultiPlayer/configuration.yaml --run-id=TetrisMultiPlayer --resume
 --To resume training for the agent that takes in 2 CNNs as input run the command: mlagents-learn Assets/results/TetrisMultiPlayer/configuration.yaml --run-id=TetrisMultiPlayer2Inputs --resume
 --Press the play button in the Unity editor.

The ProjectTetris folder contains all of the resources used to build the Project Tetris video game.

The ProjectTetrisBuild folder contains the final build of the game.