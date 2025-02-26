using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//A disc is a ball in 2d space
public class Disc
{
    public float x, y, vx, vy, radius, mass;

    public Color color;

    //Getters
    //Left border of the AABB belonging to the disc
    public float Left => this.x - this.radius;
    //Right border of the AABB belonging to the disc
    public float Right => this.x + this.radius;



    public Disc(float x, float y, float vx, float vy, float radius)
    {
        //Position
        this.x = x;
        this.y = y;

        //Velocity
        this.vx = vx;
        this.vy = vy;
        
        this.radius = radius;
        
        this.mass = Mathf.PI * radius * radius;
    }
    


    //Move the sphere by integrating one step pos = pos + dt * vel
    //Check for collision with map border
    public void Update(float dt)
    {
        //Move the ball
        this.x += this.vx * dt;
        this.y += this.vy * dt;
    }
}
