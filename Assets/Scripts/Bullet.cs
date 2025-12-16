using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    /**
     * Bullet handles projectile behavior and collision with asteroids
     *  Added ship ownership tracking for proper asteroid destruction credits
     */
    public class Bullet : MonoBehaviour
    {
        private float speed = 10f;
        private Vector3 direction;
        private float lifetime = 3f; // Bullet disappears after 3 seconds
        private float spawnTime;
        private Ship ownerShip; // FIXED: Track which ship fired this bullet

        void Start()
        {
            spawnTime = Time.time;
        }

        void Update()
        {
            // Move the bullet
            transform.position += direction * speed * Time.deltaTime;

            // Destroy bullet after lifetime expires
            if (Time.time - spawnTime > lifetime)
            {
                Destroy(gameObject);
            }
        }

        /**
         * Initialize the bullet with direction, speed, and owner
         * FIXED: Added ownerShip parameter
         */
        public void Init(Vector3 _direction, float _speed = 10f, Ship _owner = null)
        {
            direction = _direction.normalized;
            speed = _speed;
            ownerShip = _owner;
        }

        /**
         * OnTriggerEnter2D handles collision with asteroids
         *  Now notifies the owner ship when an asteroid is destroyed
         */
        void OnTriggerEnter2D(Collider2D col)
        {
            if (col.gameObject.CompareTag("Asteroid"))
            {
                // Notify the owner ship of the successful hit
                if (ownerShip != null)
                {
                    ownerShip.OnAsteroidDestroyed();
                }

                // Destroy both the bullet and the asteroid
                Destroy(col.gameObject);
                Destroy(gameObject);
            }
        }
    }
}