using System;
using System.Linq;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class SkreduinoConnector : MonoBehaviour
{
    const int BindAxisByte = 0;
    const int CallActionByte = 1;
    const int SetVarByte = 2;
    const int AxisByte = 3;
    const string ConnectionCallbackAnswer = "OK";

    public string handshakeCode = "";
    public bool VerboseDebugging = false;
    public List<SkreduinoAction> Actions = new List<SkreduinoAction>();

    private Dictionary<string, int> Axis = new Dictionary<string, int>();
    private Action QueuedActions = () => { };
    private SerialPort Port = null;
    private CancellationTokenSource ReadingPort = null;

    private void Awake()
    {
        ConnectPorts();
    }

    private void LateUpdate()
    {
        var actions = QueuedActions;
        QueuedActions = () => { };
        actions.Invoke();
    }

    private async Task ConnectPorts()
    {
        var allPorts = SerialPort.GetPortNames();

        var attemptedPorts = new List<SerialPort>();

        foreach (var portName in allPorts)
        {
            try
            {
                var port = new SerialPort(portName, 9600);
                port.Open();
                port.DiscardInBuffer();
                port.DiscardOutBuffer();
                port.Write($"{handshakeCode}\n");
                attemptedPorts.Add(port);
            }
            catch
            {

            }
        }

        await Task.Delay(15);

        foreach (var attemptedPort in attemptedPorts)
        {
            try
            {
                attemptedPort.ReadTimeout = 10;
                var fullMessage = attemptedPort.ReadExisting();
                var messages = fullMessage.Split('\n');

                int i = 0;
                for (i = 0; i < messages.Length; i++)
                {
                    var message = messages[i].Trim(new char[] { (char)10, (char)13 });
                    if (message == ConnectionCallbackAnswer)
                    {
                        Port = attemptedPort;
                        if (VerboseDebugging)
                            Debug.Log($"Conectado a porta {Port.PortName}");
                        break;
                    } else
                    {
                        Debug.Log(message);
                    }
                }
                if (Port != null)
                {
                    for (int j = i+1; j < messages.Length; j++)
                    {
                        InterpretMessage(messages[j]);
                    }
                    break;
                }
            }
            catch
            {

            }
        }

        foreach (var attemptedPort in attemptedPorts)
        {
            try
            {
                if (attemptedPort != Port)
                    attemptedPort.Close();
            }
            catch { }
        }

        if (Port != null)
        {
            ReadingPort = new CancellationTokenSource();
            _ = Task.Run(ReadPort, ReadingPort.Token);
        }
    }

    private void ReadPort()
    {
        try
        {
            Port.ReadTimeout = SerialPort.InfiniteTimeout;
            while (Port != null && Port.IsOpen)
            {
                var message = Port.ReadLine();
                InterpretMessage(message);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            Port = null;
        }
    }

    private void InterpretMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        switch (message[0])
        {
            case (char)BindAxisByte:
                {
                    var payload = message.Substring(1);
                    var parsedPayload = payload.Split(';');
                    if (parsedPayload.Length != 2)
                        break;
                    UpdateAxis(parsedPayload[0], int.Parse(parsedPayload[1]));
                }
                break;
            case (char)CallActionByte:
                {
                    var payload = message.Substring(1);
                    CallAction(payload);
                }             
                break;
            default:
                if (VerboseDebugging)
                    Debug.Log($"Mensagem recebida do Skreduino: {message}");
                break;
        }
    }

    private void UpdateAxis(string key, int value)
    {
        if (!Axis.ContainsKey(key))
            Axis.Add(key, value);
        else
            Axis[key] = value;

        if (VerboseDebugging)
            Debug.Log($"Axis {key} alterado para {value}");
    }

    private void CallAction(string key)
    {
        var action = Actions.FirstOrDefault(a => a.actionIdentifier == key);
        if (action == null)
        {
            if (VerboseDebugging)
                Debug.Log($"Não existe ação com nome {key}");
        } else
        {
            QueuedActions += () => action.actionEvent.Invoke();
        }
    }

    public int GetAxis(string key)
    {
        if (!Axis.ContainsKey(key))
            Axis.Add(key, 0);
        return Axis[key];
    }

    private void OnDestroy()
    {
        if (Port != null && Port.IsOpen)
        {
            ReadingPort.Cancel();
            Port.Close();
        }
    }
}

[System.Serializable]
public class SkreduinoAction
{
    public string actionIdentifier;
    public UnityEvent actionEvent;
}