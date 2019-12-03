using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }

    public Tilemap Tilemap = null;
    public List<Player> Team1 = new List<Player>();
    public List<Player> Team2 = new List<Player>();
    public TrackTransform camera = null;
    public Player ActivePlayer = null;
    public IEnumerator<Player> CurrentTeam = null;
    public IEnumerator<IEnumerator<Player>> Teams = null;
    private int Team1Index = -1;
    private int Team2Index = -1;
    private int CTeam = 0;
    public Text Text;
    
    private void Start()
    {
        //Teams = new List<List<Player>>{Team1, Team2}.GetEnumerator();
        Teams = new List<IEnumerator<Player>> {Team1.GetEnumerator(), Team2.GetEnumerator()}.GetEnumerator();
        //Teams.MoveNext();

        Team1.ForEach(player => player.Team = Team1);
        Team2.ForEach(player => player.Team = Team2);
        CTeam = 2;
        //CurrentTeam = Teams.Current.GetEnumerator();
        //PlayerTurn(NextPlayer());
        NextPlayer();
    }

    public void PlayerTurn(Player player)
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
        //bomb.ExplodeEvent.AddListener(() => StartCoroutine(DelaySeconds(0.5f, () => PlayerTurn(NextPlayer()))));
        bomb.ExplodeEvent.AddListener(() => StartCoroutine(DelaySeconds(1f, NextPlayer)));
    }

    public void NextPlayer()
    {
        if (!Team1.Any(t => t.gameObject.activeSelf))
        {
            var obj = new GameObject();
            obj.transform.position = new Vector3(0,0);
            camera.Target = obj.transform;
            Text.enabled = true;
            Text.text = "Time 2 Venceu!";
            return;
        }
        if (!Team2.Any(t => t.gameObject.activeSelf))
        {
            var obj = new GameObject();
            obj.transform.position = new Vector3(0,0);
            camera.Target = obj.transform;
            Text.enabled = true;
            Text.text = "Time 1 Venceu!";
            return;
        }
        
        Teams.MoveNext();
        if (Teams.Current == null)
        {
            Teams.Reset();
            Teams.MoveNext();
        }

        CurrentTeam = Teams.Current; //.GetEnumerator();
        Player player = null;
        int count = 0;
        while (player == null)
        {
            if (count > Team1.Count+Team2.Count)
            {
                break;
            }
            count++;
            CurrentTeam.MoveNext();
            if (CurrentTeam.Current == null)
            {
                CurrentTeam.Reset();
            } else if (CurrentTeam.Current.gameObject.activeSelf)
            {
                player = CurrentTeam.Current;
            }
        }

        //return CurrentTeam.Current;
        PlayerTurn(CurrentTeam.Current);
    }

    private IEnumerator DelaySeconds(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action.Invoke();
    }
}
