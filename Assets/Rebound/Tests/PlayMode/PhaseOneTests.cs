using System.Collections;
using NUnit.Framework;
using Rebound;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class PhaseOneTests
{
    GameManager game;
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return SceneManager.LoadSceneAsync("Game");
        game = Object.FindObjectOfType<GameManager>();
        game.player.readKeyboard = false;
        game.spawner.enabled = false;
        game.player.GetComponent<WeaponController>().enabled = false;
    }

    [UnityTest]
    public IEnumerator MovementRiseGravityAndBounds()
    {
        float startX = game.player.transform.position.x;
        game.player.SetMoveAxis(1);
        game.player.RequestRise();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Assert.Greater(game.player.VerticalSpeed, 4f);
        yield return new WaitForSeconds(0.3f);
        Assert.Greater(game.player.transform.position.x, startX + 1f);
        yield return new WaitForSeconds(0.6f);
        Assert.LessOrEqual(game.player.transform.position.x, game.PlayerMaxX + 0.05f);
        Assert.Less(game.player.VerticalSpeed, 0f);
        game.player.SetMoveAxis(-1);
        for (int i = 0; i < 6; i++)
        {
            game.player.RequestRise();
            yield return new WaitForSeconds(0.28f);
        }
        Assert.GreaterOrEqual(game.player.transform.position.x, game.PlayerMinX - 0.05f);
        Assert.LessOrEqual(game.player.transform.position.y, game.TopY + 0.05f);
        yield return new WaitForSeconds(1.2f);
        Assert.GreaterOrEqual(game.player.VerticalSpeed, -game.settings.maxFallSpeed - 0.001f);
    }

    [UnityTest]
    public IEnumerator EnergyUsesDistanceAndReboundOnlyHitsFront()
    {
        game.player.enabled = false;
        game.player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        var energy = game.energy;
        energy.ResetTracking(4, true);
        energy.ObserveHeight(5);
        Assert.AreEqual(0, energy.Energy);
        energy.ObserveHeight(3);
        Assert.AreEqual(44, energy.Energy, 0.001f);
        Assert.IsFalse(energy.TryConsume());
        energy.ObserveHeight(1);
        Assert.AreEqual(88, energy.Energy, 0.001f);
        Vector2 origin = game.player.transform.position;
        var front = game.spawner.SpawnAt(origin + Vector2.right * 2);
        var behind = game.spawner.SpawnAt(origin + Vector2.left * 2);
        var far = game.spawner.SpawnAt(origin + Vector2.right * 7.5f);
        Physics2D.SyncTransforms();
        Assert.IsTrue(game.player.rebound.TryFire());
        Assert.AreEqual(0, energy.Energy);
        Assert.AreEqual(1, game.Kills);
        Assert.AreEqual(game.settings.enemyHp, behind.Hp);
        Assert.AreEqual(game.settings.enemyHp, far.Hp);
        yield return null;
        Assert.IsTrue(front == null);
    }

    [UnityTest]
    public IEnumerator AutomaticFireKillsAndPoolReuses()
    {
        game.player.enabled = false;
        game.player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        game.player.GetComponent<WeaponController>().enabled = true;
        game.spawner.SpawnAt((Vector2)game.player.transform.position + Vector2.right * 3);
        var pool = Object.FindObjectOfType<ProjectilePool>();
        int initial = pool.CreatedCount;
        yield return new WaitForSeconds(0.9f);
        Assert.GreaterOrEqual(game.Kills, 1);
        game.player.GetComponent<WeaponController>().enabled = false;
        yield return new WaitForSeconds(game.settings.projectileLifetime + 0.15f);
        Assert.AreEqual(initial, pool.CreatedCount);
        Assert.AreEqual(initial, pool.AvailableCount);
        var first = pool.Fire(Vector2.zero);
        first.Release(); first.Release();
        Assert.AreEqual(initial, pool.AvailableCount, "Duplicate release must not duplicate pooled entries.");
    }

    [UnityTest]
    public IEnumerator FallDamageInvulnerabilityGameOverAndRetry()
    {
        var body = game.player.GetComponent<Rigidbody2D>();
        for (int i = 0; i < 3; i++)
        {
            body.position = new Vector2(-5, game.FallY - 1);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(2 - i, game.health.CurrentHp);
            Assert.AreEqual(game.SafePosition.x, body.position.x, 0.01f);
            Assert.Greater(body.position.y, game.SafePosition.y - 0.1f);
            Assert.AreEqual(0, game.energy.Energy, 0.1f);
            if (i == 0) Assert.IsFalse(game.health.TakeDamage(), "Respawn should block contact damage.");
        }
        Assert.IsTrue(game.IsGameOver);
        float elapsed = game.SurvivalTime;
        yield return new WaitForSeconds(0.1f);
        Assert.AreEqual(elapsed, game.SurvivalTime);
        game.Retry();
        yield return null; yield return null;
        game = Object.FindObjectOfType<GameManager>();
        Assert.IsFalse(game.IsGameOver);
        Assert.AreEqual(3, game.health.CurrentHp);
        Assert.AreEqual(0, game.Kills);
        Assert.Less(game.SurvivalTime, 0.5f);
    }

    [UnityTest]
    public IEnumerator RiseCooldownAndEnemyContact()
    {
        game.player.RequestRise();
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        yield return new WaitForSeconds(0.08f);
        float speedBefore = game.player.VerticalSpeed;
        game.player.RequestRise();
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        Assert.Less(game.player.VerticalSpeed, speedBefore, "Cooldown must reject a second rise.");
        yield return new WaitForSeconds(0.22f);
        game.player.RequestRise();
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        Assert.Greater(game.player.VerticalSpeed, 4f);
        game.player.enabled = false;
        game.player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        game.spawner.SpawnAt(game.player.transform.position);
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        Assert.AreEqual(2, game.health.CurrentHp);
        game.spawner.SpawnAt(game.player.transform.position);
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        Assert.AreEqual(2, game.health.CurrentHp);
    }

    [UnityTest]
    public IEnumerator RealDescentChargesAndRiseConsumesEnergy()
    {
        var body = game.player.GetComponent<Rigidbody2D>();
        body.position = new Vector2(-5, 3);
        body.velocity = Vector2.zero;
        game.energy.ResetTracking(3, true);
        yield return new WaitForSeconds(1.05f);
        Assert.GreaterOrEqual(game.energy.Energy, 65);
        game.player.RequestRise();
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        Assert.Less(game.energy.Energy, 1);
        Assert.Greater(game.player.VerticalSpeed, 4f);
    }
}
