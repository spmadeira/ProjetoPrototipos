using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameController : MonoBehaviour
{
    private static GameController _instance;
    public static GameController Instance => _instance;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public Tilemap Tilemap = null;
    public List<Player> Team1 = new List<Player>();
    public List<Player> Team2 = new List<Player>();
    public TrackTransform camera = null;
    public Player ActivePlayer = null;
    public IEnumerator<Player> CurrentTeam = null;
    public IEnumerator<List<Player>> Teams = null;

    //Refazer tudo isso pro fluxo fazer sentido.
    //Jogador recebe controle e camera
    //Jogador move.
    //Jogador atira bomba, perde controle e camera, bomba ganha camera
    //Bomba vai e explode. Bomba perde camera.
    //Outro jogador recebe bomba e camera.
    private void Start()
    {
        Teams = new List<List<Player>>{Team1, Team2}.GetEnumerator();
        //Teams.MoveNext();

        Team1.ForEach(player => player.Team = Team1);
        Team2.ForEach(player => player.Team = Team2);
        
        //CurrentTeam = Teams.Current.GetEnumerator();
        PlayerTurn(NextPlayer());
    }

    private void PlayerTurn(Player player)
    {
        ActivePlayer = player;
        player.CurrentEnergy = 100;
        player.playerState = Player.PlayerState.Moving;
        camera.Target = player.transform;
        player.ShootBombEvent.AddListener(bomb =>
        {
            FollowBomb(bomb);
            ActivePlayer = null;
            player.ShootBombEvent.RemoveAllListeners();
        });
    }

    private void FollowBomb(Bomb bomb)
    {
        camera.Target = bomb.transform;
        bomb.ExplodeEvent.AddListener(() => StartCoroutine(DelaySeconds(0.5f, () => PlayerTurn(NextPlayer()))));
    }

    private Player NextPlayer()
    {
        CurrentTeam.MoveNext();

        if (CurrentTeam.Current != null)
        {
            //PlayerTurn(CurrentTeam.Current);
            return CurrentTeam.Current;
        }
        else
        {
            Teams.MoveNext();
            if (Teams.Current == null)
            {
                Teams.Reset();
                Teams.MoveNext();
            }

            CurrentTeam = Teams.Current.GetEnumerator();
            CurrentTeam.MoveNext();
            //PlayerTurn(CurrentTeam.Current);
            return CurrentTeam.Current;
        }
    }

    private IEnumerator DelaySeconds(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action.Invoke();
    }
}
