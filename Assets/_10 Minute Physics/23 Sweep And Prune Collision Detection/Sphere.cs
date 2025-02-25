using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sphere
{
    public float x, y, vx, vy, radius, mass;

    public Color color;

    //Getters
    public float Left => this.x - this.radius;

    public float Right => this.x + this.radius;



    public Sphere(float x, float y, float vx, float vy, float radius)
    {
        this.x = x;
        this.y = y;
        this.vx = vx;
        this.vy = vy;
        this.radius = radius;
        this.mass = Mathf.PI * radius * radius;
        //this.color = `hsl(${ Math.random() * 360}, 70 %, 50 %)`;
    }
    


    public void Update(float dt, float mapSizeX, float mapSizeY)
    {
        //Move the ball
        this.x += this.vx * dt;
        this.y += this.vy * dt;

        //Check if the ball ended outside of the map
        //If so move it inside and invert the vel component
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

    /*
    draw()
    {
        ctx.beginPath();
        ctx.arc(this.x, this.y, this.radius, 0, Math.PI * 2);
        ctx.fillStyle = this.color;
        ctx.fill();
        ctx.closePath();
    }
    */
}
