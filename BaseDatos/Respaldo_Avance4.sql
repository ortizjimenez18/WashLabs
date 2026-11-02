DECLARE @directorio nvarchar(4000)=CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'));
DECLARE @archivo nvarchar(4000)=@directorio+N'\WashLabs_Avance4_Auditoria_20261031.bak';
BACKUP DATABASE WashLabsDB TO DISK=@archivo WITH COPY_ONLY,CHECKSUM;
RESTORE VERIFYONLY FROM DISK=@archivo WITH CHECKSUM;
