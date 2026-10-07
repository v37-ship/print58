package com.wanggaspa.receipt

import androidx.room.*

@Entity(tableName = "transactions")
data class Tx(
    @PrimaryKey(autoGenerate = true) val id: Long = 0,
    val date: String, val time: String,
    val customer: String, val customerPhone: String = "",
    val promoPhone: String = "", // holds the promo code since v2
    val itemsJson: String, val subtotal: Long, val total: Long,
    val paymentStatus: String
)

@Entity(tableName = "services")
data class Svc(
    @PrimaryKey val name: String,
    val desc: String, val price: Long
)

@Dao
interface TxDao {
    @Query("SELECT * FROM transactions ORDER BY id DESC") suspend fun all(): List<Tx>
    @Insert suspend fun insert(t: Tx)
    @Query("SELECT COALESCE(SUM(total),0) FROM transactions WHERE date=:date") suspend fun dailyTotal(date: String): Long
}

@Dao
interface SvcDao {
    @Query("SELECT * FROM services ORDER BY name") suspend fun all(): List<Svc>
    @Insert(onConflict = OnConflictStrategy.REPLACE) suspend fun upsert(s: Svc)
    @Query("DELETE FROM services WHERE name=:n") suspend fun delete(n: String)
}

@Database(entities = [Tx::class, Svc::class], version = 2)
abstract class AppDb : RoomDatabase() {
    abstract fun tx(): TxDao
    abstract fun svc(): SvcDao
}
