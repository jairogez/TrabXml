<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="2.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:fo="http://www.w3.org/1999/XSL/Format" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:fn="http://www.w3.org/2005/xpath-functions">
	<xsl:template match="/">
		<xsl:for-each select="//topic">
			<xsl:variable name="pageName">
				<xsl:value-of select="concat(@id, '.html')"/>
			</xsl:variable>
			<xsl:variable name="id">
				<xsl:value-of select="@id"/>
			</xsl:variable>
			<xsl:result-document href="{$pageName}">
				<html>
					<head>
						<title>
							<xsl:value-of select="baseName/baseNameString"/>
						</title>
					</head>
					<body>
						<b>
							<font size="10">
								<xsl:value-of select="baseName/baseNameString"/>
							</font>
						</b>
						<br/>
						<xsl:for-each select="occurrence">
							<xsl:variable name="hrefOcurrence">
								<xsl:value-of select="substring(scope/topicRef/@href, 2)"/>
							</xsl:variable>
							<dl>
								<dt>
									<xsl:value-of select="../../topic[@id = $hrefOcurrence]/baseName/baseNameString"/>
								</dt>
								<dd>
									<xsl:value-of select="resourceData"/>
								</dd>
							</dl>
							<br/>
						</xsl:for-each>
						<xsl:if test="count(//association[member/topicRef[@href=concat('#', $id)]]/member/topicRef[not(@href=concat('#', $id))]) > 0">
							<br/>
							<b>Tópicos relacionados:
							</b>
						</xsl:if>
						<ul>
							<xsl:for-each select="//association[member/topicRef[@href=concat('#', $id)]]/member/topicRef[not(@href=concat('#', $id))]">
								<br/>
								<xsl:variable name="idAssociatedTopic">
									<xsl:value-of select="substring(@href, 2)"/>
								</xsl:variable>
								<li>
									<a href="{concat($idAssociatedTopic,'.html')}">
										<xsl:value-of select="//topic[@id=$idAssociatedTopic]/baseName/baseNameString"/>
									</a>
								</li>
							</xsl:for-each>
						</ul>
						<br/>
						<br/>
						<a href="index.html" style="text-align:center">
								Página inicial
							</a>
					</body>
				</html>
			</xsl:result-document>
		</xsl:for-each>
		<xsl:result-document href="index.html">
			<html>
				<head>
					<title>
						Filmes
					</title>
				</head>
				<body>
					<b>
						Lista de filmes:
					</b>
					<ul>
						<xsl:for-each select="//topic[instanceOf/topicRef[@href='#Filme']]">
							<br/>
							<li>
								<a href="{concat(@id,'.html')}">
									<xsl:value-of select="baseName/baseNameString"/>
								</a>
							</li>
						</xsl:for-each>
					</ul>
				</body>
			</html>
		</xsl:result-document>
	</xsl:template>
</xsl:stylesheet>
