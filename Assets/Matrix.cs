using UnityEngine;
using System.Collections;
using System.Collections.Generic;



public class Matrix<T> : IEnumerable<T>
{
    private T[,] _matrix;

    public Matrix(int width, int height)
    {
        _matrix = new T[width, height];
    }

    public Matrix(T[,] copyFrom)
    {
        int width = copyFrom.GetLength(0);
        int height = copyFrom.GetLength(1);
        _matrix = new T[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                _matrix[x, y] = copyFrom[x, y];
            }
        }
    }

    public Matrix<T> Clone()
    {
        int width = _matrix.GetLength(0);
        int height = _matrix.GetLength(1);
        Matrix<T> aux = new Matrix<T>(width, height);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                aux[x, y] = _matrix[x, y];
            }
        }
        return aux;
    }

    public void SetRangeTo(int x0, int y0, int x1, int y1, T item)
    {
        for (int x = x0; x <= x1; x++)
        {
            for (int y = y0; y <= y1; y++)
            {
                _matrix[x, y] = item;
            }
        }
    }

    public List<T> GetRange(int x0, int y0, int x1, int y1)
    {
        List<T> range = new List<T>();
        for (int x = x0; x <= x1; x++)
        {
            for (int y = y0; y <= y1; y++)
            {
                range.Add(_matrix[x, y]);
            }
        }
        return range;
    }

    public T this[int x, int y]
    {
        get { return _matrix[x, y]; }
        set { _matrix[x, y] = value; }
    }

    public int Width { get { return _matrix.GetLength(0); } }

    public int Height { get { return _matrix.GetLength(1); } }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (T item in _matrix)
        {
            yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
