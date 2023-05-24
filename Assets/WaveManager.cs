using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public Material WaveMat;
    public Texture2D WaveTex;
    public bool ReflectiveBoundary;


    private float[][] waveN, waveNM1, waveNP1; // state information

    private float lX = 10; //width
    private float lY = 10; //heigth
    [SerializeField] private float dx = 0.1f; //x-axis density
    private float dy { get => dx; } //y-axis density

    private int nx, ny; //resolution

    public float CFL;
    public float c = 1;
    private float dt; //timestep
    private float t; // current time
    [SerializeField] private float floatToColorMultiplier = 2; //emphasize color
    [SerializeField] private float pulseFrequency = 1;
    [SerializeField] private float pulseMagnitude = 1;
    [SerializeField] private Vector2Int pulsePosition = new(50,50);
    [SerializeField] private float elasticity = 0.98f; 


    private void Start()
    {
        nx = Mathf.FloorToInt(lX / dx);
        ny = Mathf.FloorToInt(lY / dy);
        WaveTex = new Texture2D(nx, ny, TextureFormat.RGBA32, false);

        //creates empty field
        waveN = new float[nx][];
        waveNM1 = new float[nx][];
        waveNP1 = new float[nx][];
        for (int i = 0; i < nx; i++)
        {
            waveN[i] = new float[ny];
            waveNP1[i] = new float[ny];
            waveNM1[i] = new float[ny];
        }

        WaveMat.SetTexture("_MainTex", WaveTex); //coloring Texture
        WaveMat.SetTexture("_DisplacementTex", WaveTex); //displacement Texture
    }

    private void WaveStep()
    {
        dt = CFL * dx / c; //recalculate dt
        t += dx; //increment time

        if (ReflectiveBoundary)
        {
            ApplyReflectiveBoundary();
        }
        else
        {
            ApplyAbsorptiveBoundary();
        }


        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                waveNM1[i][j] = waveN[i][j]; // copy state at N to state N-1
                waveN[i][j] = waveNP1[i][j]; // copy state at N+1 to state N 
            }
        }


        //dripping effect
        waveN[pulsePosition.x][pulsePosition.y] = dt * dt * 20 * pulseMagnitude * Mathf.Cos(t * Mathf.Rad2Deg * pulseFrequency);


        for (int i = 1; i < nx -1; i++) //do not process edges
        {
            for (int j = 1; j < ny -1; j++)
            {
                float n_ij = waveN[i][j];
                float n_ip1j = waveN[i + 1][j];
                float n_im1j = waveN[i - 1][j];
                float n_ijp1 = waveN[i][j + 1];
                float n_ijm1 = waveN[i][j - 1];
                float nm1_ij = waveNM1[i][j];
                waveNP1[i][j] = 2 * n_ij - nm1_ij + CFL * CFL * (n_ijm1 + n_ijp1 + n_im1j + n_ip1j - 4f * n_ij); //wave equation
                waveNP1[i][j] *= elasticity;
            }
        }
    }

    private void ApplyMatrixToTexture(float[][] state, ref Texture2D tex, float floatToColorMultiplier)
    {
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                float val = state[i][j] * floatToColorMultiplier;
                tex.SetPixel(i, j, new Color(val + 0.5f, val + 0.5f, val + 0.5f, 1f)); //point grayscale
            }
        }
        tex.Apply();
    }

    private void ApplyReflectiveBoundary()
    {
        for (int i = 0; i < nx; i++)
        {
            waveN[i][0] = 0f;
            waveN[i][ny-1] = 0f;
        }
        for (int j = 0; j < ny; j++)
        {
            waveN[0][j] = 0f;
            waveN[ny - 1][j] = 0f;
        }
    }

    private void ApplyAbsorptiveBoundary()
    {
        float v = (CFL - 1f) / (CFL + 1f);
        for (int i = 0; i < nx; i++)
        {
            waveNP1[i][0] = waveN[i][1] + v * (waveNP1[i][1] - waveN[i][0]);
            waveNP1[i][ny - 1] = waveN[i][ny-2] + v * (waveNP1[i][ny - 2] - waveN[i][ny - 1]);
        }
        for (int j = 0; j < ny; j++)
        {
            waveNP1[0][j] = waveN[1][j] + v * (waveNP1[1][j] - waveN[0][j]);
            waveNP1[ny - 1][j] = waveN[ny - 2][j] + v * (waveNP1[ny - 2][j] - waveN[ny - 1][j]);
        }
    }

    private void MousePositionOnTexture( ref Vector2Int pos)
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            pos = new Vector2Int((int) (hit.textureCoord.x * nx), (int) (hit.textureCoord.y * ny));
        }
    }

    private void Update()
    {
        MousePositionOnTexture(ref pulsePosition);
        WaveStep();
        ApplyMatrixToTexture(waveN, ref WaveTex, floatToColorMultiplier);
    }
}
