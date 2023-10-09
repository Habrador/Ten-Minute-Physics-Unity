using HeightFieldWaterSim;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq.Expressions;



//Simulate a swimming pool by using a height field water columns in a 2.5d grid
//TODO
//- The water mesh is currently intersecting with the wall
public class HeightFieldWaterSimController : MonoBehaviour
{
    public Material waterMaterial;
    public Material ballMaterial;
    public Material tankMaterial;

    //Parents to get a clean workspace
    public Transform environmentParent;
    public Transform ballsParent;



    private void Start()
    {
        InitScene();

        MyPhysicsScene.isPaused = false;
    }



    private void Update()
    {
        if (MyPhysicsScene.isPaused)
        {
            return;
        }

        MyPhysicsScene.waterSurface.UpdateVisMesh();
    }



    private void FixedUpdate()
    {
        Simulate();
    }



    private void InitScene()
    {
        //Init the water simulation which will also add a water mesh
        float wx = MyPhysicsScene.tankSize.x;
        float wy = MyPhysicsScene.tankSize.y;
        float wz = MyPhysicsScene.tankSize.z;
        
        float b = MyPhysicsScene.tankBorder;

        WaterSurface waterSurface = new (
            wx,
            wz,
            MyPhysicsScene.waterHeight,
            MyPhysicsScene.waterSpacing,
            waterMaterial);

        MyPhysicsScene.waterSurface = waterSurface;


        //Tank walls
        GameObject wall1 = GameObject.CreatePrimitive(PrimitiveType.Cube);

        wall1.GetComponent<MeshRenderer>().material = tankMaterial;

        GameObject wall2 = Instantiate(wall1);
        GameObject wall3 = Instantiate(wall1);
        GameObject wall4 = Instantiate(wall1);

        wall1.transform.parent = environmentParent;
        wall2.transform.parent = environmentParent;
        wall3.transform.parent = environmentParent;
        wall4.transform.parent = environmentParent;

        Vector3 wallSide1Scale = new(b, wy, wz);
        Vector3 wallSide2Scale = new(wx, wy, b);

        wall1.transform.localScale = wallSide1Scale;
        wall2.transform.localScale = wallSide1Scale;

        wall3.transform.localScale = wallSide2Scale;
        wall4.transform.localScale = wallSide2Scale;

        wall1.transform.position = new(-0.5f * wx, 0.5f * wy, 0.0f);
        wall2.transform.position = new(0.5f * wx, 0.5f * wy, 0.0f);

        wall3.transform.position = new(0.0f, 0.5f * wy, -wz * 0.5f);
        wall4.transform.position = new(0.0f, 0.5f * wy, wz * 0.5f);  


        //Balls
        Vector3 b1_pos = new(-0.5f, 1.0f, -0.5f);
        Vector3 b2_pos = new( 0.5f, 1.0f, -0.5f);
        Vector3 b3_pos = new( 0.5f, 1.0f,  0.5f);

        Material b1_mat = new(ballMaterial);
        Material b2_mat = new(ballMaterial);
        Material b3_mat = new(ballMaterial);

        b1_mat.color = Color.yellow;
        b2_mat.color = Color.blue;
        b3_mat.color = Color.red;

        HFBall b1 = new(b1_pos, 0.20f, 2.0f, b1_mat, ballsParent);
        HFBall b2 = new(b2_pos, 0.30f, 0.7f, b2_mat, ballsParent);
        HFBall b3 = new(b3_pos, 0.25f, 0.2f, b3_mat, ballsParent);

        MyPhysicsScene.objects.Add(b1);
        MyPhysicsScene.objects.Add(b2);
        MyPhysicsScene.objects.Add(b3);


        //Ground
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);

        ground.transform.position = Vector3.zero;
        ground.transform.localScale = Vector3.one * 100f;
    }



    private void Simulate()
    {
        if (MyPhysicsScene.isPaused)
        {
            return;
        }

        float dt = Time.fixedDeltaTime;

        //MyPhysicsScene.waterSurface.Simulate();

        for (int i = 0; i < MyPhysicsScene.objects.Count; i++)
        {
            HFBall obj = MyPhysicsScene.objects[i];

            obj.Simulate(dt);
            
            for (int j = 0; j < i; j++)
            {
                obj.HandleCollision(MyPhysicsScene.objects[j]);
            }
        }
    }
}