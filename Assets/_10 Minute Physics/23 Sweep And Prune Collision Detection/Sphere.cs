using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sphere
{
    public float x, y, vx, vy, radius, mass;

    public Color color;

    //Getters
    //Left border of the AABB belonging to the sphere
    public float Left => this.x - this.radius;
    //Right border of the AABB belonging to the sphere
    public float Right => this.x + this.radius;



    public Sphere(float x, float y, float vx, float vy, float radius)
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
    public void Update(float dt, float mapSizeX, float mapSizeY)
    {
        //Move the ball
        this.x += this.vx * dt;
        this.y += this.vy * dt;

        //Check if the ball ended outside of the map
        //If so move it inside and invert the vel component
        //This assumes maps bottom-left corner is at 0,0
        if (this.x - this.radius < 0f)
        {
            this.x = this.radius;
            this.vx *= -1f;
        }
        if (this.x + this.radius > mapSizeX)
        {
            this.x = mapSizeX - this.radius;
            this.vx *= -1f;
        }
        if (this.y + this.radius > mapSizeY)
        {
            this.y = mapSizeY - this.radius;
            this.vy *= -1f;
        }
        if (this.y - this.radius < 0f)
        {
            this.y = this.radius;
            this.vy *= -1f;
        }
    }
}
