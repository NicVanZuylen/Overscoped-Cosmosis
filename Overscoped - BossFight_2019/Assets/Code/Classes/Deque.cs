using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class Deque<T>
{
    private T[] m_contents;
    private int m_nCount;

    private void Expand()
    {
        T[] newContents = new T[m_contents.Length + 1];

        m_contents.CopyTo(newContents, 0);

        m_contents = newContents;
    }

    public void Shrink()
    {
        T[] newContents = new T[m_nCount];

        Array.Copy(m_contents, 0, newContents, 0, newContents.Length);

        m_contents = newContents;
    }

    public int Capacity()
    {
        return m_contents.Length;
    }

    public int Count()
    {
        return m_nCount;
    }

    public void EnqueueStart(T item)
    {
        if (m_nCount >= m_contents.Length)
            Expand();

        // Copy contents one space forward.
        Array.Copy(m_contents, 0, m_contents, 1, m_nCount);

        m_contents[0] = item;
        ++m_nCount;
    }

    public void EnqueueEnd(T item)
    {
        if (m_nCount >= m_contents.Length)
            Expand();

        m_contents[m_nCount++] = item;
    }

    public T DequeueStart(T item)
    {
        T returnItem = m_contents[0];

        Array.Copy(m_contents, 1, m_contents, 0, --m_nCount);

        return returnItem;
    }

    public T DequeueEnd(T item)
    {
        return m_contents[(m_nCount--) - 1];
    }
}
