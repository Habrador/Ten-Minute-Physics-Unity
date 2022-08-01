using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButterflyController : MonoBehaviour
{
    public GameObject[] poses;

    private int activePose = 0;



    private void Start()
    {
        foreach (GameObject go in poses)
        {
            go.SetActive(false);
        }

        poses[1].SetActive(true);

        //StartCoroutine(Flap());
    }



    public void StartResting(float restTime)
    {
        StartCoroutine(Resting(restTime));
    }



    private IEnumerator Resting(float restTime)
    {
        yield return new WaitForSeconds(restTime);

        StartCoroutine(Fly());
    }



    private IEnumerator Fly()
    {
        StartCoroutine(Flap());

        float rotationSpeed = 2f;

        float flySpeed = 5f;

        Transform targetTrans = Camera.main.transform;

        //Vector3 targetPos = targetTrans.position - targetTrans.forward * 5f + targetTrans.right * 15f + Vector3.up * 10f;
        Vector3 targetPos = targetTrans.position - targetTrans.forward * 5f + targetTrans.right * 0f - Vector3.up * 4f;

        while (true)
        {
            //Rotate towards the target
            Vector3 direction = (targetPos - transform.position).normalized;

            Quaternion lookRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);


            //Fly towards target
            transform.Translate(flySpeed * Time.deltaTime * Vector3.forward);


            yield return new WaitForSeconds(Time.deltaTime);
        }
    }



    private IEnumerator Flap()
    {
        while (true)
        {
            poses[activePose].SetActive(false);

            activePose += 1;

            activePose = activePose > 2 ? 0 : activePose;

            poses[activePose].SetActive(true);

            yield return new WaitForSeconds(0.025f);
        }
    }



    public void Create(GameObject go, Vector3 pos, Quaternion rot)
    {
        Instantiate(go, pos, rot);
    }
}
