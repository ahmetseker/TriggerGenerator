# TriggerGenerator
Data audit trigger generator for Oracle Database

If you need data auditing on Oracle database use this program to generate required triggers.<br/>

First create data audit table. Here is the create sql:<br/>
<code>
CREATE TABLE DATA_AUDIT
(
  AUDID        RAW(16) DEFAULT SYS_GUID(),
  TABLENAME    VARCHAR2(30 BYTE),
  RECID        VARCHAR2(2000 BYTE),
  OROWID       VARCHAR2(20 BYTE),
  IUDFLAG      VARCHAR2(1 BYTE),
  AUDDATE      DATE,
  USERNAME     VARCHAR2(20 BYTE),
  TERMINAL     VARCHAR2(64 BYTE),
  PROGRAMNAME  VARCHAR2(64 BYTE),
  XMLCONTENT   CLOB
);

alter table data_audit add primary key (AUDID);
</code>
Then use the trigger generator program to generate data audit triggers.
