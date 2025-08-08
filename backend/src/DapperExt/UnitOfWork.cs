using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Dapper
{
    public class UnitOfWork : IDisposable
    {
        private IDbTransaction _transaction;
        private readonly Action<UnitOfWork> _onCommit;
        private readonly Action<UnitOfWork> _onRollback;

        /// <summary>
        /// Cria uma nova instância de <see cref="UnitOfWork"/>
        /// </summary>
        /// <param name="transaction">Usado para confirmar ou reverter as instruções que estão sendo executadas dentro desta unidade de trabalho</param>
        /// <param name="onCommitOrRollback">Uma <see cref="Action{UnitOfWork}"/> que será executado quando a unidade de trabalho estiver sendo cometida ou revertida.</param>
        public UnitOfWork(IDbTransaction transaction, Action<UnitOfWork> onCommitOrRollback) : this(transaction, onCommitOrRollback, onCommitOrRollback)
        {
        }

        /// <summary>
        /// Cria uma nova instância de <see cref="UnitOfWork"/>
        /// </summary>
        /// <param name="transaction">Usado para confirmar ou reverter as instruções que estão sendo executadas dentro desta unidade de trabalho</param>
        /// <param name="onCommitOrRollback">Uma <see cref="Action{UnitOfWork}"/> que será executado quando a unidade de trabalho estiver sendo cometida ou revertida.</param>
        public UnitOfWork(IDbTransaction transaction, Action<UnitOfWork> onCommit, Action<UnitOfWork> onRollback)
        {
            _transaction = transaction;
            _onCommit = onCommit;
            _onRollback = onRollback;
        }


        // <summary>
        /// Retorna uma instancia de <see cref="IDbTransaction"/> 
        /// </summary>
        public IDbTransaction Transaction
        {
            get { return _transaction; }
        }


        /// <summary>
        /// SaveChanges tentará e confirmará todas as transações que foram executadas contra o banco de dados dentro desta unidade de trabalho.
        /// </summary>
        /// <remarks>
        /// Se o commit falhar, a transação será revertida.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Exceção lançada se o commit ou roolback já executado</exception>
        public void SaveChanges()
        {
            if (_transaction == null)
                throw new InvalidOperationException("Esta unidade de trabalho já foi salva ou desfeita.");

            try
            {
                _transaction.Commit();
                _onCommit(this);
            }
            finally
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }

        /// <summary>
        /// Implementa <see cref="IDisposable.Dispose"/>, e reverte os comandos executados dentro de uma unidade de trabalho.
        /// </summary>
        public void Dispose()
        {
            if (_transaction == null) return;

            try
            {
                _transaction.Rollback();
                _onRollback(this);
            }
            finally
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }
    }
}
