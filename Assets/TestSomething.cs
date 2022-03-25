using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSomething : MonoBehaviour
{
    // Start is called before the first frame update
    List<int> m_list;
    void Start()
    {
        m_list = new List<int>();
        m_list.Add(1);
        m_list.Add(2);

        //foreach 移除 list 会报错
        foreach (var item in m_list)
            m_list.Remove(item);

        //请在for中操作
        for (int i = 0; i < m_list.Count; i++)
            m_list.RemoveAt(i);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
