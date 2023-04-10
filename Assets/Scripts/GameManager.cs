using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private static GameState _state;
    public static event Action<GameState> OnGameStateChanged;
    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    private void Awake()
    {
        Instance = this;    
    }

    // Start is called before the first frame update
    void Start()
    {
        UpdateGameState(GameState.Freemovement);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static void UpdateGameState(GameState newState) {
        _state = newState;
        switch (newState) {
            case GameState.Freemovement:
                break;
            case GameState.Slingshot:
                break;
            case GameState.Released:
            // code to switch mode to controller mode
                break;
            case GameState.GameWon:
                break;
            case GameState.GameLostBoundsExceeded:
                break;
            case GameState.GameLostFuelDrained:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }
        OnGameStateChanged?.Invoke(newState);
        Debug.Log("Updating to GameState: " + newState);
    }

    public static GameState GetState()
    {
        return _state;
    }

    public static bool GameEnded()
    {
        return _state == GameState.GameWon ||
            _state == GameState.GameLostFuelDrained ||
            _state == GameState.GameLostBoundsExceeded;
    }

    public static bool GameLost()
    {
        return _state == GameState.GameLostFuelDrained ||
            _state == GameState.GameLostBoundsExceeded;
    }
}


public enum GameState {
    Freemovement,
    Slingshot,
    Released,
    GameWon,
    GameLostBoundsExceeded,
    GameLostFuelDrained
}