using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;


public class Sudoku : MonoBehaviour
{
    public Cell prefabCell;
    public Canvas canvas;
    public Text feedback;
    public float stepDuration = 0.05f;
    [SerializeField] [Range(1, 82)] private int difficulty = 40;
    [SerializeField][Range(2, 3)] private int customSize = 3; //Tamano del sudoku custom (dejarlo en 3)

    Matrix<Cell> _board;
    Matrix<int> _createdMatrix;
    List<int> posibles = new List<int>();
    int _smallSide;
    int _bigSide;
    string memory = "";
    string canSolve = "";
    bool canPlayMusic = false;
    List<int> nums = new List<int>();



    float r = 1.0594f;
    float frequency = 440;
    float gain = 0.5f;
    float increment;
    float phase;
    float samplingF = 48000;


    void Start()
    {
        long mem = System.GC.GetTotalMemory(true);
        feedback.text = string.Format("MEM: {0:f2}MB", mem / (1024f * 1024f));
        memory = feedback.text;
        _smallSide = customSize;
        _bigSide = _smallSide * _smallSide;
        frequency = frequency * Mathf.Pow(r, 2);
        CreateEmptyBoard();
        ClearBoard();
    }

    //Segun que entendi, pone todos los numeros en 0 y des-lockea todo
    void ClearBoard()
    {
        _createdMatrix = new Matrix<int>(_bigSide, _bigSide);
        foreach (var cell in _board)
        {
            cell.number = 0;
            cell.locked = cell.invalid = false;
        }
    }

    // Crea el sudoku (Nota: Fijarse de aca de usar el custom size)
    void CreateEmptyBoard()
    {

        //Esto ordena el como se ve visualmente, esto va perfecto para los 3
        float spacing = 68f;
        float startX = -spacing * 4f;
        float startY = spacing * 4f;

        _board = new Matrix<Cell>(_bigSide, _bigSide);
        for (int x = 0; x < _board.Width; x++)
        {
            for (int y = 0; y < _board.Height; y++)
            {
                var cell = _board[x, y] = Instantiate(prefabCell);
                cell.transform.SetParent(canvas.transform, false);
                cell.transform.localPosition = new Vector3(startX + x * spacing, startY - y * spacing, 0);
            }
        }
    }


    int watchdog = 0;
    //La funcion que resuelve recursivamente el sudoku usando el backtracking
    bool RecuSolve(Matrix<int> matrixParent, int x, int y, List<Matrix<int>> solution) // El protectMaxDepth no se que hacia, asi que lo saque
    {


        if (watchdog <= 0) return false;
        watchdog--;

        for (int i = x; i < matrixParent.Width; i++)
        {
            for (int j = 0; j < matrixParent.Height; j++)
            {
                if (matrixParent[i, j] == Cell.EMPTY)
                {
                    for (int num = 1; num <= _bigSide; num++)
                    {
                        if (CanPlaceValue(matrixParent, num, i, j))
                        {
                            matrixParent[i, j] = num;
                            solution.Add(matrixParent.Clone());

                            if (RecuSolve(matrixParent, i, j, solution))
                            {
                                return true;
                            }

                            matrixParent[i, j] = Cell.EMPTY;
                            solution.Add(matrixParent.Clone());
                        }
                    }
                    return false;
                }
            }
        }

        return true;
    }


    void OnAudioFilterRead(float[] array, int channels)
    {
        if (canPlayMusic)
        {
            increment = frequency * Mathf.PI / samplingF;
            for (int i = 0; i < array.Length; i++)
            {
                phase = phase + increment;
                array[i] = (float)(gain * Mathf.Sin((float)phase));
            }
        }

    }

    void changeFreq(int num)
    {
        frequency = 440 + num * 80;
    }

    IEnumerator ShowSequence(List<Matrix<int>> seq)
    {
        for (int step = 0; step < seq.Count; step++)
        {
            TranslateAllValues(seq[step]);
            feedback.text = $"Pasos: {step + 1}/{seq.Count} - MEM: {System.GC.GetTotalMemory(true) / (1024f * 1024f):f2}MB - {canSolve}";
            yield return new WaitForSeconds(stepDuration);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) || Input.GetMouseButtonDown(1))
            SolvedSudoku();
        else if (Input.GetKeyDown(KeyCode.C) || Input.GetMouseButtonDown(0))
            CreateSudoku();
    }

    void SolvedSudoku()
    {
        StopAllCoroutines();
        nums = new List<int>();
        var solution = new List<Matrix<int>>();
        watchdog = 100000;
        var result = RecuSolve(_createdMatrix.Clone(), 0, 0, solution);
        if (result)
        {
            StartCoroutine(ShowSequence(solution));
        }
        else
        {
            feedback.text = "No se encontró una solución válida.";
        }
        //
        long mem = System.GC.GetTotalMemory(true);
        memory = string.Format("MEM: {0:f2}MB", mem / (1024f * 1024f));
        canSolve = result ? " VALID" : " INVALID";
        //
    }

    void CreateSudoku()
    {
        StopAllCoroutines();
        nums = new List<int>();
        canPlayMusic = false;
        ClearBoard();
        watchdog = 100000;
        CreateNew(_createdMatrix);
        //GenerateValidLine(_createdMatrix);  //Usar esta linea me genera sudokus invalidos, o me los reconoce como invalidos

        //No le pude dar un uso a esto
        //List<Matrix<int>> l = new List<Matrix<int>>();
        //var result = false;
        //_createdMatrix = l[0].Clone();
        bool isValid = CanPlaceValue(_createdMatrix, _createdMatrix[0, 0], 0, 0);
        LockRandomCells();
        ClearUnlocked(_createdMatrix);
        TranslateAllValues(_createdMatrix);
        long mem = System.GC.GetTotalMemory(true);
        memory = string.Format("MEM: {0:f2}MB", mem / (1024f * 1024f));
        canSolve = isValid ? "VALID" : "INVALID";
        feedback.text = $"Pasos: {memory} - {canSolve}";
    }

    void GenerateValidLine(Matrix<int> mtx)
    {
        int[] aux = new int[9];
        for (int i = 0; i < 9; i++)
        {
            aux[i] = i + 1;
        }
        int numAux = 0;
        for (int j = 0; j < aux.Length; j++)
        {
            int r = 1 + Random.Range(j, aux.Length);
            numAux = aux[r - 1];
            aux[r - 1] = aux[j];
            aux[j] = numAux;
        }
        for (int k = 0; k < aux.Length; k++)
        {
            mtx[k, 0] = aux[k];
        }
    }


    void ClearUnlocked(Matrix<int> mtx)
    {
        for (int i = 0; i < _board.Height; i++)
        {
            for (int j = 0; j < _board.Width; j++)
            {
                if (!_board[j, i].locked)
                    mtx[j, i] = Cell.EMPTY;
            }
        }
    }

    void LockRandomCells() //VERIFICA SI HAY UN NUMERO ANTES, SI HAY UN NUMERO SE PUEDE LOCKEAR, SI ESTA VACIO NO
    {
        List<Vector2> posibles = new List<Vector2>();
        for (int y = 0; y < _board.Height; y++)
        {
            for (int x = 0; x < _board.Width; x++)
            {
                if (!_board[x, y].locked && _createdMatrix[x, y] != Cell.EMPTY)
                {
                    posibles.Add(new Vector2(x, y));
                }
            }
        }
        for (int i = 0; i < posibles.Count; i++)
        {
            Vector2 temp = posibles[i];
            int randomIndex = Random.Range(i, posibles.Count);
            posibles[i] = posibles[randomIndex];
            posibles[randomIndex] = temp;
        }
        int cellsToLock = posibles.Count - difficulty;
        for (int i = 0; i < cellsToLock; i++)
        {
            Vector2 cellPosition = posibles[i];
            _board[(int)cellPosition.x, (int)cellPosition.y].locked = true;
        }
    }

    void TranslateAllValues(Matrix<int> matrix)
    {
        for (int y = 0; y < _board.Height; y++)
        {
            for (int x = 0; x < _board.Width; x++)
            {
                _board[x, y].number = matrix[x, y];
            }
        }
    }

    void TranslateSpecific(int value, int x, int y)
    {
        _board[x, y].number = value;
    }

    void TranslateRange(int x0, int y0, int xf, int yf)
    {
        for (int x = x0; x < xf; x++)
        {
            for (int y = y0; y < yf; y++)
            {
                _board[x, y].number = _createdMatrix[x, y];
            }
        }
    }

    void CreateNew(Matrix<int> board)
    {
        //CreateRandomSudoku(board); // FUNCION QUE CREA EL SUDOKU, PARA NO USAR SUDOKUS YA PRE EXISTENTES

        Matrix<int> solution = null;
        var solutions = new List<Matrix<int>>();
        do
        {
            ClearBoard();
            GenerateValidLine(board);
            solution = board.Clone();
            RecuSolve(solution, 0, 0, solutions);
            if (solutions.Count > 0)
            {
                solution = solutions[solutions.Count - 1];
            }
        }
        while (solution == null);

        _createdMatrix = solution;

        TranslateAllValues(_createdMatrix);
    }

    //Hay algo aca que rompe el tamaño, seguramente tengo que rescribir esta funcion esta para que funque (si se deja en 3 funca joya)
    bool CanPlaceValue(Matrix<int> mtx, int value, int x, int y)
    {
        List<int> fila = new List<int>();
        List<int> columna = new List<int>();
        List<int> area = new List<int>();
        List<int> total = new List<int>();

        Vector2 cuadrante = Vector2.zero;

        for (int i = 0; i < mtx.Height; i++)
        {
            for (int j = 0; j < mtx.Width; j++)
            {
                if (i != y && j == x) columna.Add(mtx[j, i]);
                else if (i == y && j != x) fila.Add(mtx[j, i]);
            }
        }

        // Calcular las coordenadas del cuadrante
        cuadrante.x = (int)(x / 3) * 3;
        cuadrante.y = (int)(y / 3) * 3;

        // Obtener los valores del area
        for (int i = (int)cuadrante.x; i < (int)cuadrante.x + 3; i++)
        {
            for (int j = (int)cuadrante.y; j < (int)cuadrante.y + 3; j++)
            {
                area.Add(mtx[i, j]);
            }
        }

        // Combinar las listas de las filas, columnas y el area para verificar duplicados
        total.AddRange(fila);
        total.AddRange(columna);
        total.AddRange(area);
        total = FilterZeros(total);

        // Verifiacion para saber si el sudoku es valido
        for (int i = 0; i < mtx.Width; i++)
        {
            if (i != x && mtx[i, y] == value) return false;  // Vuelve fallado
            if (i != y && mtx[x, i] == value) return false;  // Vuelve fallado
        }

        int startX = (x / 3) * 3;
        int startY = (y / 3) * 3;

        for (int i = startX; i < startX + 3; i++)
        {
            for (int j = startY; j < startY + 3; j++) // Vuelve fallado
            {
                if (i != x && j != y && mtx[i, j] == value) return false;
            }
        }

        // Vuelve joya
        return true;
    }



    List<int> FilterZeros(List<int> list)
    {
        List<int> aux = new List<int>();
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != 0) aux.Add(list[i]);
        }
        return aux;
    }
}
