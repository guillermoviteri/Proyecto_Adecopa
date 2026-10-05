using System;

[Serializable]
public class DatosJuego
{
    public Config config = new Config();
    public Continente[] continentes;
}

[Serializable]
public class Config
{
    public int[] preguntasPorNivel = { 6, 7, 8, 9, 10 };
    public bool elegirAlAzar = true;
    public int indicadorInicialMin = 40;
    public int indicadorInicialMax = 55;
    public int limiteColapso = 15;
    public int puntosParaCorrecto = 70;
    public int castigoPorError = 10;
    public int castigoExtraPorNivel = 2;
    public int castigoPorRacha = 3;
    public bool castigoEnDecisiones = false;
}

[Serializable]
public class Continente
{
    public string clave;
    public string nombre;
    public string escena;
    public string color;
    public string pais;
    public string crisis;
    public Ejercicio[] ejercicios;

    public string Escena => string.IsNullOrEmpty(escena) ? clave : escena;
}

[Serializable]
public class Ejercicio
{
    public int nivelMin;
    public string tipo;
    public string titulo;
    public string texto;
    public string quien;
    public string dice;
    public string[] materias;
    public string[] competencias;
    public Opcion[] opciones;

    public bool EsDecision => !string.IsNullOrEmpty(tipo) && tipo.Trim().ToLower().StartsWith("decis");
    public int Nivel => nivelMin <= 0 ? 1 : nivelMin;
}

[Serializable]
public class Opcion
{
    public string texto;
    public int puntos;
    public string explicacion;
    public string efectos;
    public string luego;
    public string efectosLuego;
}
